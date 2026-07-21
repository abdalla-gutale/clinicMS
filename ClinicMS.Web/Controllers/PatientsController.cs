using ClinicMS.Web.Filters;
using ClinicMS.Web.Models.Api.Patients;
using ClinicMS.Web.Services.Api;
using Microsoft.AspNetCore.Mvc;

namespace ClinicMS.Web.Controllers
{
    [RequireAuth]
    public class PatientsController : Controller
    {
        // Content-type is spoofable by the client, so we also whitelist by extension and re-check the
        // file's actual magic bytes below -- three independent checks before anything touches disk.
        private static readonly Dictionary<string, byte[]> AllowedImageSignatures = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".jpeg"] = new byte[] { 0xFF, 0xD8, 0xFF },
            [".png"] = new byte[] { 0x89, 0x50, 0x4E, 0x47 },
            [".webp"] = new byte[] { 0x52, 0x49, 0x46, 0x46 },
            [".gif"] = new byte[] { 0x47, 0x49, 0x46, 0x38 },
        };
        private const long MaxPhotoBytes = 3 * 1024 * 1024;
        private const int DefaultPageSize = 8;

        private readonly IPatientsApiClient _patientsApiClient;
        private readonly IWebHostEnvironment _environment;

        public PatientsController(IPatientsApiClient patientsApiClient, IWebHostEnvironment environment)
        {
            _patientsApiClient = patientsApiClient;
            _environment = environment;
        }

        [RequirePermission("/patients", PermissionAction.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var initialPage = await _patientsApiClient.GetPagedAsync(1, DefaultPageSize, null, null, cancellationToken);
            ViewBag.PatientsPageJson = ViewJson.Serialize(initialPage);
            return View();
        }

        [HttpGet]
        [RequirePermission("/patients", PermissionAction.View)]
        public async Task<IActionResult> GetPage(int page, int pageSize, string? search, PatientGender? gender, CancellationToken cancellationToken)
        {
            var result = await _patientsApiClient.GetPagedAsync(page, pageSize, search, gender, cancellationToken);
            return Json(result);
        }

        [HttpPost]
        [RequirePermission("/patients", PermissionAction.Create)]
        public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsApiClient.CreateAsync(request, cancellationToken);
                return Json(patient);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/patients", PermissionAction.Edit)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var patient = await _patientsApiClient.UpdateAsync(id, request, cancellationToken);
                return Json(patient);
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/patients", PermissionAction.Delete)]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _patientsApiClient.DeleteAsync(id, cancellationToken);
                return Json(new { success = true });
            }
            catch (ApiException ex)
            {
                return StatusCode(ex.StatusCode, new { message = ex.Message });
            }
        }

        [HttpPost]
        [RequirePermission("/patients", PermissionAction.Edit)]
        public async Task<IActionResult> UploadPhoto(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return StatusCode(400, new { message = "No file was uploaded." });
            }
            if (file.Length > MaxPhotoBytes)
            {
                return StatusCode(400, new { message = "Photo must be 3 MB or smaller." });
            }

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageSignatures.TryGetValue(extension, out var signature))
            {
                return StatusCode(400, new { message = "Only JPG, PNG, WEBP or GIF images are allowed." });
            }

            var header = new byte[signature.Length];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header, cancellationToken);
                if (read < signature.Length || !header.AsSpan(0, signature.Length).SequenceEqual(signature))
                {
                    return StatusCode(400, new { message = "File content does not match a supported image format." });
                }
            }

            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "patients");
            Directory.CreateDirectory(uploadsDir);

            // Random filename -- never trust the client-supplied name for the path we write to disk.
            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var destination = new FileStream(filePath, FileMode.Create))
            {
                await using var source = file.OpenReadStream();
                await source.CopyToAsync(destination, cancellationToken);
            }

            return Json(new { url = $"/uploads/patients/{fileName}" });
        }
    }
}
