using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet.Models;
using Microsoft.Identity.Web;
using WebAppOpenIDConnectDotNet;
using Azure.Storage.Blobs;
using System.Text;
using System.IO;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Sas;

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        ITokenAcquisition _tokenAcquisition;

        public HomeController(ITokenAcquisition tokenAcquisition)
        {
            _tokenAcquisition = tokenAcquisition;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Demo using AAD to connect Azure Blob storage";
            return View();
        }

        [AuthorizeForScopes(Scopes = new string[] { "https://storage.azure.com/user_impersonation" })]
        public IActionResult Blob()
        {
            ViewData["Message"] = "Upload a file";
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> BlobUpload(IFormFile file)
        {
            string filename = "no file recevied";
            if (file != null)
                {
                    filename = file.FileName;
                    string message = await CreateBlob(new TokenAcquisitionTokenCredential(_tokenAcquisition), file);
                }
                        
            return Json(new {filename = filename});
        }

        public async Task<JsonResult> GetSAS(FileModel model)
        {
            Uri uri = await GetUserDelegationSasBlob(new TokenAcquisitionTokenCredential(_tokenAcquisition), model.filename);
            return Json(new {URI= uri});
        }


        private static async Task<string> CreateBlob(TokenAcquisitionTokenCredential tokenCredential, IFormFile file)
        {
            Uri blobUri = new Uri("https://cshuicantonresturant.blob.core.windows.net/testclient/" + file.FileName);
            BlobClient blobClient = new BlobClient(blobUri, tokenCredential);

            // string blobContents = "Blob created by Azure AD authenticated user.";
            // byte[] byteArray = Encoding.ASCII.GetBytes(blobContents);

            // using (MemoryStream ms = new MemoryStream())
            using(var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            return "Blob successfully created";
        }

        public class FileModel
        {
            public string filename { get; set; }
        }
        private static async Task<Uri> GetUserDelegationSasBlob(TokenAcquisitionTokenCredential tokenCredential, string blobname)
        {
            // BlobServiceClient blobServiceClient =
            //     blobClient.GetParentBlobContainerClient().GetParentBlobServiceClient();
            Uri blobUri = new Uri("https://cshuicantonresturant.blob.core.windows.net");
            var blobServiceClient = new BlobServiceClient(blobUri, tokenCredential);
            var containerClient = blobServiceClient.GetBlobContainerClient("testclient");
            var blobClient = containerClient.GetBlobClient(blobname);
            // Get a user delegation key for the Blob service that's valid for 7 days.
            // You can use the key to generate any number of shared access signatures 
            // over the lifetime of the key.
            var userDelegationKey =
                await blobServiceClient.GetUserDelegationKeyAsync(DateTimeOffset.UtcNow,
                                                                DateTimeOffset.UtcNow.AddDays(7));

            // Create a SAS token that's also valid for 7 days.
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = blobClient.BlobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(7)
            };

            // Specify read and write permissions for the SAS.
            sasBuilder.SetPermissions(BlobSasPermissions.Read |
                                    BlobSasPermissions.Write);

            // Add the SAS token to the blob URI.
            BlobUriBuilder blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
            {
                // Specify the user delegation key.
                Sas = sasBuilder.ToSasQueryParameters(userDelegationKey, 
                                                    blobServiceClient.AccountName)
            };

            Console.WriteLine("Blob user delegation SAS URI: {0}", blobUriBuilder);
            Console.WriteLine();
            return blobUriBuilder.ToUri();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
