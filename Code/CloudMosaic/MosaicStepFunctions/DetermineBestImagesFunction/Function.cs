using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using MosaicStepFunctions.Common;

using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

using Amazon.S3;

using ImageMagick;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace DetermineBestImagesFunction
{
    public class Function
    {
        public IAmazonS3 S3Client { get; set; }
        public IAmazonDynamoDB DDBClient { get; set; }

        public Function()
        {
            this.DDBClient = new AmazonDynamoDBClient();
            this.S3Client = new AmazonS3Client();
        }

        public async Task<State> FunctionHandler(State state, ILambdaContext context)
        {
            var mosaicLayoutInfo = await MosaicLayoutInfoManager.Load(this.S3Client, state.Bucket, state.MosaicLayoutInfoKey);

            var tileInfos = await LoadGalleryItems(state.GalleryId, context);

            Dictionary<string, int> s3KeyToId = new Dictionary<string, int>();
            mosaicLayoutInfo.IdToTileKey = new Dictionary<int, string>();
            mosaicLayoutInfo.TileMap = new int[mosaicLayoutInfo.ColorMap.GetLength(0), mosaicLayoutInfo.ColorMap.GetLength(1)];

            context.Logger.LogLine($"Determing best fit for each tile: {mosaicLayoutInfo.ColorMap.GetLength(0)}x{mosaicLayoutInfo.ColorMap.GetLength(1)}");
            for(int x = 0; x < mosaicLayoutInfo.ColorMap.GetLength(0); x++)
            {
                for(int y = 0; y < mosaicLayoutInfo.ColorMap.GetLength(1); y++)
                {
                    var bestFit = DetermineBestImage(mosaicLayoutInfo.ColorMap[x, y], tileInfos);

                    int id;
                    if(!s3KeyToId.TryGetValue(bestFit.TileKey, out id))
                    {
                        id = s3KeyToId.Count + 1;
                        s3KeyToId[bestFit.TileKey] = id;
                        mosaicLayoutInfo.IdToTileKey[id] = bestFit.TileKey;
                    }
                    mosaicLayoutInfo.TileMap[x, y] = id;
                }
            }

            await MosaicLayoutInfoManager.Save(S3Client, state.Bucket, mosaicLayoutInfo);
            context.Logger.LogLine($"Saving mosaic layout info to {mosaicLayoutInfo.Key}");

            return state;
        }

        private TileImageInfo DetermineBestImage(MagickColor targetColor, IList<TileImageInfo> tileInfos)
        {
            int r, g, b;

            var differences = new List<Tuple<double, int>>();

            MagickColor[] passColor = new MagickColor[4];
            for (int i = 0; i < tileInfos.Count(); i++)
            {
                passColor[0] = tileInfos[i].AverageTL;
                passColor[1] = tileInfos[i].AverageTR;
                passColor[2] = tileInfos[i].AverageBL;
                passColor[3] = tileInfos[i].AverageBR;

                r = passColor[0].R + passColor[1].R + passColor[2].R + passColor[3].R;
                g = passColor[0].G + passColor[1].G + passColor[2].G + passColor[3].G;
                b = passColor[0].B + passColor[1].B + passColor[2].B + passColor[3].B;

                r = Math.Abs(targetColor.R - (r / 4));
                g = Math.Abs(targetColor.G - (g / 4));
                b = Math.Abs(targetColor.B - (b / 4));

                var difference = r + g + b;
                difference /= 3 * 255;

                differences.Add(new Tuple<double, int>(difference, i));

            }

            var sortedList = differences.OrderBy(i => i.Item1).ToList();
            var item = sortedList[new Random().Next((int)(differences.Count * .05))];

//            tileInfos[item.Item2].Data.Add(new Point(x, y));
            return tileInfos[item.Item2];
        }

        private async Task<List<TileImageInfo>> LoadGalleryItems(string galleryId, ILambdaContext context)
        {
            var request = new QueryRequest
            {
                TableName = "GalleryItems",
                ConsistentRead = false,
                KeyConditionExpression = "GalleryId = :id",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    {":id", new AttributeValue{S = galleryId} }
                }
            };

            var tileInfos = new List<TileImageInfo>();

            int page = 1;
            QueryResponse response = null;
            do
            {
                request.ExclusiveStartKey = response?.LastEvaluatedKey;

                response = await this.DDBClient.QueryAsync(request);
                context.Logger.LogLine($"Loaded page {page++} with {response.Items.Count} items");

                foreach(var item in response.Items)
                {
                    var tile = new TileImageInfo
                        {
                            TileKey = item["TileKey"].S,
                            AverageTL = ConvertDDBMapToColor(item["TL"].M),
                            AverageTR = ConvertDDBMapToColor(item["TR"].M),
                            AverageBL = ConvertDDBMapToColor(item["BL"].M),
                            AverageBR = ConvertDDBMapToColor(item["BR"].M)
                        };

                    tileInfos.Add(tile);
                }

            } while (response.LastEvaluatedKey?.Count > 0);

            return tileInfos;
        }

        private MagickColor ConvertDDBMapToColor(Dictionary<string, AttributeValue> map)
        {
            var c = new MagickColor();
            c.R = ushort.Parse(map["R"].N);
            c.G = ushort.Parse(map["G"].N);
            c.B = ushort.Parse(map["B"].N);
            return c;
        }
    }
}
