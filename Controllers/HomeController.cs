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
            ViewData["Message"] = "Try upload a file";
            return View();
        }
        [HttpPost]
        public async Task<JsonResult> BlobUpload(IFormFile file)
        {
            string message = await CreateBlob(new TokenAcquisitionTokenCredential(_tokenAcquisition));
            return Json(new {data="hihi"});
        }


        private static async Task<string> CreateBlob(TokenAcquisitionTokenCredential tokenCredential)
        {
            Uri blobUri = new Uri("https://cshuicantonresturant.blob.core.windows.net/testclient/Blob1.txt");
            BlobClient blobClient = new BlobClient(blobUri, tokenCredential);

            string blobContents = "Blob created by Azure AD authenticated user.";
            byte[] byteArray = Encoding.ASCII.GetBytes(blobContents);

            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            return "Blob successfully created";
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
