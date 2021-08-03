using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using CMS_QRLabelPrinter.Models;
using System.Data.SqlClient;

namespace CMS_QRLabelPrinter.Controllers
{
    public class HomeController : Controller
    {

        // Bitmap bit = null; // Global variable to store QR code as a bitmap image
        bool customerInfo = false; // Variable to determine whether to print additional customer info

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(string qrText)
        {

            string constr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CMS_db;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            using (SqlConnection conn = new SqlConnection(constr))
            {
                string query = "INSERT INTO [QRTable] (JobKeyID, ItemKeyID, ScanDate) VALUES (1, 1, GETDATE())";

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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
