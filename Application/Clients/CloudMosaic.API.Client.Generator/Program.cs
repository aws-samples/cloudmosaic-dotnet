using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CloudMosaic.API.Client.Generator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var document = await OpenApiDocument.FromUrlAsync("http://CM-Re-LoadB-951OIWEPC9JZ-1463354109.us-west-2.elb.amazonaws.com/swagger/v1/swagger.json");

            var settings = new CSharpClientGeneratorSettings
            {                
//                ClassName = "MosaicClient",
                CSharpGeneratorSettings =
                {
                    Namespace = "CloudMosaic.API.Client",
                }
            };

            var generator = new CSharpClientGenerator(document, settings);
            var code = generator.GenerateFile();
            var fullPath = DetermienFullFilePath("MosaicClient.cs");
            File.WriteAllText(fullPath, code);            
        }

        static string DetermienFullFilePath(string codeFile)
        {
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while(!string.Equals(dir.Name, "Clients"))
            {
                dir = dir.Parent;
            }

            return Path.Combine(dir.FullName, "CloudMosaic.API.Client", codeFile);
        }
    }
}
