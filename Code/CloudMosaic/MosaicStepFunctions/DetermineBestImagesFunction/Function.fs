module DetermineBestImagesFunction

open System


open Amazon.Lambda.Core

open MosaicStepFunctions.Common

open Amazon.DynamoDBv2
open Amazon.DynamoDBv2.Model

open Amazon.S3

open SixLabors.ImageSharp.PixelFormats
open System.Collections.Generic


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[<assembly: LambdaSerializer(typeof<Amazon.Lambda.Serialization.Json.JsonSerializer>)>]
()

type TileImageInfo =
    {
        TileKey:string
        AverageTL:Rgba32
        AverageTR:Rgba32
        AverageBL:Rgba32
        AverageBR:Rgba32
    }

type TileDifference = 
    {
        Difference: double;
        Tile : TileImageInfo;
    }

let random = new Random()

let ddbClient = new AmazonDynamoDBClient();
let s3Client = new AmazonS3Client();

let ConvertDDBMapToColor (map: Dictionary<string, AttributeValue>) =
    new Rgba32(Byte.Parse(map.["R"].N), Byte.Parse(map.["G"].N), Byte.Parse(map.["B"].N))


let LoadGalleryItems (tableName: string) (galleryId: string) (_: ILambdaContext) = async {

    let request = new QueryRequest(
                        TableName = tableName, 
                        ConsistentRead = false, 
                        KeyConditionExpression = "GalleryId = :id"                                                        
                        )
    request.ExpressionAttributeValues.[":id"] <- new AttributeValue(S = galleryId)

    let tileInfos = new List<TileImageInfo>()

    let rec executeQuery() = async {
        let! response = ddbClient.QueryAsync(request) |> Async.AwaitTask

        for item in response.Items do
            let tile = {
                            TileKey = item.["TileKey"].S;
                            AverageTL = ConvertDDBMapToColor(item.["TL"].M);
                            AverageTR = ConvertDDBMapToColor(item.["TR"].M);
                            AverageBL = ConvertDDBMapToColor(item.["BL"].M);
                            AverageBR = ConvertDDBMapToColor(item.["BR"].M);
                        }
            tileInfos.Add(tile)

        if response.LastEvaluatedKey.Count > 0 then
            request.ExclusiveStartKey <- response.LastEvaluatedKey
            do! executeQuery()
    }

    do! executeQuery()
    return tileInfos
}


let DetermineBestImage (targetColor:Rgba32) (tileInfos:IList<TileImageInfo>) =

    let computeDifference (tileInfo:TileImageInfo) =
        let ar = ((int)(tileInfo.AverageTL.R) + (int)(tileInfo.AverageTR.R) + (int)(tileInfo.AverageBL.R) + (int)(tileInfo.AverageBR.R)) / 4
        let ag = ((int)(tileInfo.AverageTL.G) + (int)(tileInfo.AverageTR.G) + (int)(tileInfo.AverageBL.G) + (int)(tileInfo.AverageBR.G)) / 4
        let ab = ((int)(tileInfo.AverageTL.B) + (int)(tileInfo.AverageTR.B) + (int)(tileInfo.AverageBL.B) + (int)(tileInfo.AverageBR.B)) / 4

        let dr = Math.Abs((int)(targetColor.R) - ar)
        let dg = Math.Abs((int)(targetColor.G) - ag)
        let db = Math.Abs((int)(targetColor.B) - ab)

        ((float)dr + (float)dg + (float)db) / (3.0 * 255.0)

    let randomIndex = random.Next((int)((float)tileInfos.Count * 0.05))
    (Array.init<TileDifference> tileInfos.Count 
                            (fun x -> 
                                let tileInfo = tileInfos.[x]
                                {Difference = (computeDifference tileInfo); Tile = tileInfo}
                            )
                            |> Array.sortBy(fun x -> x.Difference)).[randomIndex].Tile

let Process (state: State) (context: ILambdaContext) = async {

    let! mosaicLayoutInfo = MosaicLayoutInfoManager.Load(s3Client, state.Bucket, state.MosaicLayoutInfoKey) |> Async.AwaitTask
    let! tileInfos = LoadGalleryItems state.TableGalleryItems  state.GalleryId  context
    sprintf "Loaded %d tile gallery items" tileInfos.Count |> context.Logger.LogLine

    let s3KeyToId = new Dictionary<string, int>()
    mosaicLayoutInfo.IdToTileKey <- new Dictionary<int, string>()

    sprintf "Determing best fit for each tile: %dx%d" (mosaicLayoutInfo.ColorMap.GetLength(0)) (mosaicLayoutInfo.ColorMap.GetLength(1))  |> context.Logger.LogLine
    mosaicLayoutInfo.TileMap <- Array2D.init<int>  
                                    (mosaicLayoutInfo.ColorMap.GetLength(0)) 
                                    (mosaicLayoutInfo.ColorMap.GetLength(1)) 
                                    (fun row col -> 
                                        let bestFit = DetermineBestImage (mosaicLayoutInfo.ColorMap.[row, col]) tileInfos

                                        let mutable id = 0
                                        if(not (s3KeyToId.TryGetValue(bestFit.TileKey, &id))) then
                                            id <- s3KeyToId.Count + 1
                                            s3KeyToId.[bestFit.TileKey] <- id
                                            mosaicLayoutInfo.IdToTileKey.[id] <- bestFit.TileKey

                                        id
                                    )

                                        
    sprintf "Saving mosaic layout info to %s" mosaicLayoutInfo.Key |> context.Logger.LogLine
    do! MosaicLayoutInfoManager.Save(s3Client, state.Bucket, mosaicLayoutInfo) |> Async.AwaitTask   
}

let FunctionHandler (state: State) (context: ILambdaContext) =
    Process state context |> Async.RunSynchronously
    state
