using System;
using System.Diagnostics;
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

        /// <summary>
        /// Default index page displaying one input box for the IDs and two separate buttons for printing out
        /// QR codes without or without the accompanying customer's address.
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// POST function to receive the submitted form, print the QR Code and break down the
        /// provided number into JobKeyID and ItemKeyID and save them into the database along
        /// with the time of scan.
        /// </summary>
        /// <param name="qrText">Barcode to be converted and printed in QR format</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string qrText)
        {
            // Perform a validation check to make sure it is a valid ID combination before proceeding to print
            if (!string.IsNullOrWhiteSpace(qrText) && qrText.Length == 11)
            {

                customerInfo = false;

                uint jobKeyID = Convert.ToUInt32(qrText.Substring(0, 8));
                uint itemKeyID = Convert.ToUInt16(qrText.Substring(8));
                GenerateQRCode(jobKeyID, itemKeyID);
                PrintQRCode();

                // Local database path
                string constr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CMS_db;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

                // SQL command to insert the already parsed and split ID keys along with current DateTime in QRTable
                using (SqlConnection conn = new SqlConnection(constr))
                {
                    string query = $"INSERT INTO [QRTable] (JobKeyID, ItemKeyID, ScanDate) VALUES ({jobKeyID}, {itemKeyID}, GETDATE())";

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

            return Content("Barcode Error: The barcode should only contain exactly 11 digits.");
        }

        /// <summary>
        /// POST function to repond to AJAX function allowing the user to print QR code
        /// along with customer's name and address. No changes will be saved into the database.
        /// </summary>
        /// <param name="qrText">Barcode to be converted and printed in QR format</param>
        [HttpPost]
        public IActionResult CustomerInfo(string qrText)
        {
            // Perform a validation check to make sure it is a valid ID combination before proceeding to print
            if (!string.IsNullOrWhiteSpace(qrText) && qrText.Length == 11)
            {
                customerInfo = true;
                uint jobKeyID = Convert.ToUInt32(qrText.Substring(0, 8));
                uint itemKeyID = Convert.ToUInt16(qrText.Substring(8));
                GenerateQRCode(jobKeyID, itemKeyID);
                PrintQRCode();
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

        /// <summary>
        /// QR generator helper function powered by QRCoder library
        /// </summary>
        /// <param name="jobKeyID">Job ID taken from the barcode</param>
        /// <param name="itemKeyID">Item ID parsed from the barcode</param>
        private void GenerateQRCode(uint jobKeyID, uint itemKeyID)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode($@"https://mywebsite.com/QRCode?JobKeyID={jobKeyID}&ItemKeyID={itemKeyID}", QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            bit = qrCode.GetGraphic(20);
        }

        /// <summary>
        /// QR printer sending the command to the default printer
        /// </summary>
        private void PrintQRCode()
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += PrintPage;
            pd.Print();
            pd.Dispose();
        }

        /// <summary>
        /// Print the page with or without the customer information depending on the 'customerInfo' boolean
        /// </summary>
        private void PrintPage(object o, PrintPageEventArgs e)
        {
            e.Graphics.DrawImage(bit, 0, 0, bit.Height / 5, bit.Width / 5);

            // Add customer's name and address to the print out if required by the user
            if (customerInfo)
            {
                e.Graphics.DrawString("Castlecary Road 1\n" +
                    "CMS Enviro Ltd", new Font("Calibri", 15, FontStyle.Bold), Brushes.Black, 170.0f, 60.0f);
            }

        }
        #endregion
    }
}
