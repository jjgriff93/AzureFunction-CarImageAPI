using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CarImageAPI
{
    public static class GetCarImage
    {
        [FunctionName("GetCarImage")]
        public static HttpResponseMessage Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "GetCarImage/{CarReg}/{ImageAngle}")]HttpRequestMessage request, string CarReg, string ImageAngle, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            //Get the query parameters for requested image size
            string jsonContent = request.Content.ReadAsStringAsync().Result;
            Dictionary<string, int> jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonContent);
            //Initialise ints
            int requestedHeight = 0;
            int requestedWidth = 0;
            
            //If image height and width parameters are specified in the request body, set the new image size parameters
            if (jsonDictionary != null)
            {
                if (jsonDictionary.ContainsKey("height"))
                {
                    requestedHeight = jsonDictionary["height"];
                }
                if (jsonDictionary.ContainsKey("width"))
                {
                    requestedWidth = jsonDictionary["width"];
                }
            }

            //Set up link to blob storage for stored car images (for production code, consider moving this SAS token to KeyVault to abstract credentials away from code level)
            string storageConnectionString = "DefaultEndpointsProtocol=https;"
                + "AccountName=YOUR-ACCOUNT-NAME"//<<< Add your account name here
                + ";AccountKey=YOUR-ACCOUNT-KEY"//<<< Add your account key here
                + ";EndpointSuffix=core.windows.net";
            CloudStorageAccount blobAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = blobAccount.CreateCloudBlobClient();
            

            // Fetch the Car Reg & Image File Name from the path parameters in the request URL and retrieve image
            if (ImageAngle != null && CarReg != null)
            {
                //Get reference to specific car's container from Car Reg (converts to lower case as container names must be lower case)
                CloudBlobContainer blobContainer = blobClient.GetContainerReference(CarReg.ToLower());
                //Get reference to image block blob image from ImageFileName parameter the user passed in (images must be in jpg format in the blob service for this to work)
                CloudBlockBlob cloudBlockBlob = blobContainer.GetBlockBlobReference(ImageAngle +".jpg");
                //Download the image
                MemoryStream streamIn = new MemoryStream();
                cloudBlockBlob.DownloadToStream(streamIn);
                Image originalImage = Bitmap.FromStream(streamIn);

                MemoryStream streamOut = new MemoryStream();

                //If image size parameters were specified in request, send the image to our resizer method before adding to the output stream
                if (jsonDictionary != null)
                {
                    //Pass the image and requested file size into our image resizing method to resize the image
                    Image resizedImage = ResizeImage(originalImage, requestedWidth, requestedHeight);
                    
                    //Change image back to a stream for passing through to user as Http Content
                    resizedImage.Save(streamOut, ImageFormat.Png);
                    streamOut.Position = 0;
                }
                else
                {
                    originalImage.Save(streamOut, ImageFormat.Png);
                    streamOut.Position = 0;
                }

                //Create the Http response message with the resized image
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StreamContent(streamOut);
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpg");
                return response;
            }
            else
            {
                return request.CreateResponse(HttpStatusCode.BadRequest, "Please include a valid Car Reg and Image Angle (the view of the car you want - i.e. front/rear/left-side/right-side in your request URL, in the following format: API-URL/{CarReg}/{ImageAngle}");
            }
        }

        //Method to resize images without distortion
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
