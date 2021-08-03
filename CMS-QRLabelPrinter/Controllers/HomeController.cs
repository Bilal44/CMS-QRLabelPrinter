using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CMS_QRLabelPrinter.Models;
using System.Data.SqlClient;
using QRCoder;
using System.Drawing;
using System.Drawing.Printing;

namespace CMS_QRLabelPrinter.Controllers
{
    public class HomeController : Controller
    {

        Bitmap bit = null; // Global variable to store QR code as a bitmap image
        bool customerInfo = false; // Variable to determine whether to print additional customer info

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string qrText)
        {

            customerInfo = false;
            GenerateQRCode(qrText);

            string jobKeyID = qrText.Substring(0, 8);
            string itemKeyID = qrText.Substring(8);

            string constr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CMS_db;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "INSERT INTO [QRTable] (JobKeyID, ItemKeyID, ScanDate) VALUES (" + jobKeyID + ", " + itemKeyID + ", GETDATE())";

                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = conn;

                    try
                    {
                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException e)
                    {
                        return Content("SQL Error: " + e.Message.ToString());
                    }

                }
            }

            return View();
        }

        [HttpPost]
        public IActionResult CustomerInfo(string qrText)
        {
            if (!string.IsNullOrWhiteSpace(qrText) && qrText.Length == 11)
            {
                customerInfo = true;
                GenerateQRCode(qrText);
                Print();
                return StatusCode(200);
            }

            return StatusCode(400, "Error: Invalid Job and/or Item ID.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        #region Helpers

        private void GenerateQRCode(string qrText)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(@"https://mywebsite.com/" + qrText, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            bit = qrCode.GetGraphic(20);
        }

        private void Print()
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;
            pd.Print();
            pd.Dispose();
        }

        private void PrintPage(object o, PrintPageEventArgs e)
        {
            e.Graphics.DrawImage(bit, 0, 0, bit.Height / 5, bit.Width / 5);

            if (customerInfo)
            {
                e.Graphics.DrawString("Castlecary Road 1\n" +
                    "CMS Enviro Ltd", new Font("Calibri", 15), Brushes.Black, 150.0f, 60.0f);
            }

        }
        #endregion
    }
}
