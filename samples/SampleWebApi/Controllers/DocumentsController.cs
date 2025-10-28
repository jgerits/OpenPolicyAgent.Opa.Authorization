using Microsoft.AspNetCore.Mvc;
using OpenPolicyAgent.Opa.Authorization;

namespace SampleWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ILogger<DocumentsController> _logger;
    private static readonly List<Document> _documents = new()
    {
        new Document { Id = 1, Title = "Public Document", Content = "This is public", IsPublic = true },
        new Document { Id = 2, Title = "Private Document", Content = "This is private", IsPublic = false },
        new Document { Id = 3, Title = "Confidential Document", Content = "Top secret", IsPublic = false }
    };

    public DocumentsController(ILogger<DocumentsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all documents. OPA policy will determine which documents the user can see.
    /// </summary>
    [OpaAuthorize]
    [HttpGet]
    public IActionResult GetAll()
    {
        _logger.LogInformation("Getting all documents for user {User}", User.Identity?.Name);
        return Ok(_documents);
    }

    /// <summary>
    /// Get a specific document by ID. Uses custom policy path.
    /// </summary>
    [OpaAuthorize("authz/documents/read")]
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var document = _documents.FirstOrDefault(d => d.Id == id);
        if (document == null)
        {
            return NotFound();
        }

        _logger.LogInformation("Getting document {Id} for user {User}", id, User.Identity?.Name);
        return Ok(document);
    }

    /// <summary>
    /// Create a new document. Only admins should be allowed by OPA policy.
    /// </summary>
    [OpaAuthorize]
    [HttpPost]
    public IActionResult Create([FromBody] CreateDocumentRequest request)
    {
        var document = new Document
        {
            Id = _documents.Max(d => d.Id) + 1,
            Title = request.Title,
            Content = request.Content,
            IsPublic = request.IsPublic
        };

        _documents.Add(document);
        _logger.LogInformation("Created document {Id} by user {User}", document.Id, User.Identity?.Name);
        
        return CreatedAtAction(nameof(GetById), new { id = document.Id }, document);
    }

    /// <summary>
    /// Delete a document. Only admins should be allowed by OPA policy.
    /// </summary>
    [OpaAuthorize]
    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var document = _documents.FirstOrDefault(d => d.Id == id);
        if (document == null)
        {
            return NotFound();
        }

        _documents.Remove(document);
        _logger.LogInformation("Deleted document {Id} by user {User}", id, User.Identity?.Name);
        
        return NoContent();
    }
}

public record Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
}

public record CreateDocumentRequest(string Title, string Content, bool IsPublic);
