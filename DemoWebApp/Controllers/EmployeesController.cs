#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoClassLibrary;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Azure.Storage.Queues;
using System.Text;


namespace DemoWebApp.Views
{
    public class EmployeesController : Controller
    {
        private readonly DemoDbContext _context;
        private IConfiguration _configuration;

        public EmployeesController(DemoDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: Employees
        public async Task<IActionResult> Index()
        {
            return View(await _context.Employees.ToListAsync());
        }

        // GET: Employees/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employees/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Employees/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,EmpName,Salary,ImageUrl,ThumbnailUrl")] Employee employee, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                employee.ImageUrl = UploadImage(imageFile);
                _context.Add(employee);
                await _context.SaveChangesAsync();
                PostMessageToQueue(employee.Id, employee.ImageUrl);
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmpName,Salary,ImageUrl,ThumbnailUrl")] Employee employee)
        {
            if (id != employee.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(employee);
        }

        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EmployeeExists(int id)
        {
            return _context.Employees.Any(e => e.Id == id);
        }

        public string UploadImage(IFormFile imageFile)
        {
            if (imageFile == null)
            {
                return string.Empty;
            }
            BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration.GetConnectionString("StorageConnectionString"));
            BlobContainerClient container = blobServiceClient.GetBlobContainerClient("empimages");
            container.CreateIfNotExists();
            string blobName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);

            var blob = container.GetBlobClient(blobName);
            using (var stream = imageFile.OpenReadStream())
            {
                blob.Upload(stream);
            }           
            return blob.Uri.AbsoluteUri;
        }

        private void PostMessageToQueue(int empId, string imageUrl)
        {
            var queueClient = new QueueClient(_configuration.GetConnectionString("StorageConnectionString"), "thumbnailrequest");
            queueClient.CreateIfNotExists();
            BlobInformation blobInfo = new BlobInformation() { EmpId = empId, BlobUri = new Uri(imageUrl) };
            string blobString = ToBase64(blobInfo);
            if (queueClient.Exists())
            {
                //var message = JsonConvert.SerializeObject(blobString);
                queueClient.SendMessage(blobString);
            }
        }
        public string ToBase64(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] bytes = Encoding.Default.GetBytes(json);
            return Convert.ToBase64String(bytes);
        }

    }
}
