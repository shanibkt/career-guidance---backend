using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LearningVideosController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LearningVideosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET /api/learningvideos
        [HttpGet]
        [AllowAnonymous]
        public IActionResult GetAllVideos()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, skill_name, video_title, video_description, 
                           youtube_video_id, duration_minutes, thumbnail_url,
                           CASE WHEN transcript IS NOT NULL AND LENGTH(TRIM(transcript)) > 0 
                                THEN 1 ELSE 0 END as has_transcript
                    FROM learning_videos
                    ORDER BY skill_name";

                using MySqlCommand cmd = new(query, conn);
                using var reader = cmd.ExecuteReader();

                var videos = new List<object>();
                while (reader.Read())
                {
                    videos.Add(new
                    {
                        id = reader.GetInt32("id"),
                        skillName = reader.GetString("skill_name"),
                        videoTitle = reader.GetString("video_title"),
                        videoDescription = reader.IsDBNull(reader.GetOrdinal("video_description")) 
                            ? "" 
                            : reader.GetString("video_description"),
                        youtubeVideoId = reader.GetString("youtube_video_id"),
                        durationMinutes = reader.GetInt32("duration_minutes"),
                        thumbnailUrl = reader.IsDBNull(reader.GetOrdinal("thumbnail_url"))
                            ? $"https://img.youtube.com/vi/{reader.GetString("youtube_video_id")}/maxresdefault.jpg"
                            : reader.GetString("thumbnail_url"),
                        hasTranscript = reader.GetInt32("has_transcript") == 1
                    });
                }

                return Ok(new { videos });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching learning videos: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch learning videos", details = ex.Message });
            }
        }

        // GET /api/learningvideos/skills
        [HttpGet("skills")]
        [AllowAnonymous]
        public IActionResult GetVideosBySkills([FromQuery] string skills)
        {
            try
            {
                if (string.IsNullOrEmpty(skills))
                {
                    return BadRequest(new { error = "Skills parameter is required" });
                }

                var skillList = JsonSerializer.Deserialize<List<string>>(skills);
                if (skillList == null || skillList.Count == 0)
                {
                    return BadRequest(new { error = "Invalid skills format" });
                }

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Build parameterized query for multiple skills
                var parameters = new List<MySqlParameter>();
                var placeholders = new List<string>();
                
                for (int i = 0; i < skillList.Count; i++)
                {
                    var paramName = $"@skill{i}";
                    placeholders.Add(paramName);
                    parameters.Add(new MySqlParameter(paramName, skillList[i]));
                }

                string query = $@"
                    SELECT id, skill_name, video_title, video_description, 
                           youtube_video_id, duration_minutes, thumbnail_url
                    FROM learning_videos
                    WHERE skill_name IN ({string.Join(",", placeholders)})
                    ORDER BY FIELD(skill_name, {string.Join(",", placeholders)})";

                using MySqlCommand cmd = new(query, conn);
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }

                using var reader = cmd.ExecuteReader();
                var videos = new List<object>();

                while (reader.Read())
                {
                    var videoId = reader.GetString("youtube_video_id");
                    videos.Add(new
                    {
                        id = reader.GetInt32("id"),
                        skillName = reader.GetString("skill_name"),
                        videoTitle = reader.GetString("video_title"),
                        videoDescription = reader.IsDBNull(reader.GetOrdinal("video_description")) 
                            ? "" 
                            : reader.GetString("video_description"),
                        youtubeVideoId = videoId,
                        durationMinutes = reader.GetInt32("duration_minutes"),
                        thumbnailUrl = reader.IsDBNull(reader.GetOrdinal("thumbnail_url"))
                            ? $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg"
                            : reader.GetString("thumbnail_url")
                    });
                }

                return Ok(new { videos });
            }
            catch (JsonException)
            {
                return BadRequest(new { error = "Invalid JSON format for skills parameter" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching videos by skills: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch videos", details = ex.Message });
            }
        }

        // GET /api/learningvideos/{skillName}
        [HttpGet("{skillName}")]
        [AllowAnonymous]
        public IActionResult GetVideoBySkill(string skillName)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, skill_name, video_title, video_description, 
                           youtube_video_id, duration_minutes, thumbnail_url
                    FROM learning_videos
                    WHERE skill_name = @skillName
                    LIMIT 1";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@skillName", skillName);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var videoId = reader.GetString("youtube_video_id");
                    var video = new
                    {
                        id = reader.GetInt32("id"),
                        skillName = reader.GetString("skill_name"),
                        videoTitle = reader.GetString("video_title"),
                        videoDescription = reader.IsDBNull(reader.GetOrdinal("video_description")) 
                            ? "" 
                            : reader.GetString("video_description"),
                        youtubeVideoId = videoId,
                        durationMinutes = reader.GetInt32("duration_minutes"),
                        thumbnailUrl = reader.IsDBNull(reader.GetOrdinal("thumbnail_url"))
                            ? $"https://img.youtube.com/vi/{videoId}/maxresdefault.jpg"
                            : reader.GetString("thumbnail_url")
                    };

                    return Ok(video);
                }

                return NotFound(new { error = $"No video found for skill: {skillName}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching video by skill: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch video", details = ex.Message });
            }
        }

        // PUT /api/learningvideos/{id}/transcript
        // Admin endpoint to update video transcript
        [HttpPut("{id}/transcript")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateVideoTranscript(int id, [FromBody] TranscriptUpdateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Transcript))
                {
                    return BadRequest(new { error = "Transcript content is required" });
                }

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // First check if video exists
                string checkQuery = "SELECT COUNT(*) FROM learning_videos WHERE id = @id";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count == 0)
                {
                    return NotFound(new { error = $"Video with ID {id} not found" });
                }

                // Update transcript
                string updateQuery = @"
                    UPDATE learning_videos 
                    SET transcript = @transcript, 
                        updated_at = CURRENT_TIMESTAMP 
                    WHERE id = @id";

                using MySqlCommand updateCmd = new(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.Parameters.AddWithValue("@transcript", request.Transcript);

                int rowsAffected = updateCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"✅ Transcript updated for video ID: {id} ({request.Transcript.Length} characters)");
                    return Ok(new { 
                        success = true, 
                        message = "Transcript updated successfully",
                        transcriptLength = request.Transcript.Length
                    });
                }

                return StatusCode(500, new { error = "Failed to update transcript" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating transcript: {ex.Message}");
                return StatusCode(500, new { error = "Failed to update transcript", details = ex.Message });
            }
        }

        // GET /api/learningvideos/{id}/transcript
        // Get transcript for a specific video
        [HttpGet("{id}/transcript")]
        [AllowAnonymous]
        public IActionResult GetVideoTranscript(int id)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT youtube_video_id, video_title, transcript 
                    FROM learning_videos 
                    WHERE id = @id 
                    LIMIT 1";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var hasTranscript = !reader.IsDBNull(reader.GetOrdinal("transcript"));
                    var transcript = hasTranscript ? reader.GetString("transcript") : null;

                    return Ok(new
                    {
                        videoId = reader.GetString("youtube_video_id"),
                        videoTitle = reader.GetString("video_title"),
                        hasTranscript = hasTranscript && !string.IsNullOrWhiteSpace(transcript),
                        transcript = transcript,
                        transcriptLength = transcript?.Length ?? 0
                    });
                }

                return NotFound(new { error = $"Video with ID {id} not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching transcript: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch transcript", details = ex.Message });
            }
        }

        // POST /api/learningvideos
        // Create new learning video (Admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateVideo([FromBody] VideoCreateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SkillName) || 
                    string.IsNullOrWhiteSpace(request.VideoTitle) ||
                    string.IsNullOrWhiteSpace(request.YoutubeVideoId))
                {
                    return BadRequest(new { error = "Skill name, video title, and YouTube video ID are required" });
                }

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if skill already exists
                string checkQuery = "SELECT COUNT(*) FROM learning_videos WHERE skill_name = @skillName";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@skillName", request.SkillName);
                
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count > 0)
                {
                    return BadRequest(new { error = $"Video for skill '{request.SkillName}' already exists" });
                }

                string insertQuery = @"
                    INSERT INTO learning_videos (
                        skill_name, video_title, video_description, youtube_video_id, 
                        duration_minutes, thumbnail_url, transcript
                    ) VALUES (
                        @skillName, @videoTitle, @videoDescription, @youtubeVideoId,
                        @durationMinutes, @thumbnailUrl, @transcript
                    )";

                using MySqlCommand insertCmd = new(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@skillName", request.SkillName);
                insertCmd.Parameters.AddWithValue("@videoTitle", request.VideoTitle);
                insertCmd.Parameters.AddWithValue("@videoDescription", request.VideoDescription ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@youtubeVideoId", request.YoutubeVideoId);
                insertCmd.Parameters.AddWithValue("@durationMinutes", request.DurationMinutes);
                insertCmd.Parameters.AddWithValue("@thumbnailUrl", request.ThumbnailUrl ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@transcript", request.Transcript ?? (object)DBNull.Value);

                int rowsAffected = insertCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    long newId = insertCmd.LastInsertedId;
                    Console.WriteLine($"✅ Video created: {request.SkillName} (ID: {newId})");
                    return Ok(new { 
                        success = true, 
                        message = "Video created successfully",
                        id = newId
                    });
                }

                return StatusCode(500, new { error = "Failed to create video" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating video: {ex.Message}");
                return StatusCode(500, new { error = "Failed to create video", details = ex.Message });
            }
        }

        // PUT /api/learningvideos/{id}
        // Update learning video (Admin only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdateVideo(int id, [FromBody] VideoUpdateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SkillName) || 
                    string.IsNullOrWhiteSpace(request.VideoTitle) ||
                    string.IsNullOrWhiteSpace(request.YoutubeVideoId))
                {
                    return BadRequest(new { error = "Skill name, video title, and YouTube video ID are required" });
                }

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if video exists
                string checkQuery = "SELECT COUNT(*) FROM learning_videos WHERE id = @id";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                
                var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                if (count == 0)
                {
                    return NotFound(new { error = $"Video with ID {id} not found" });
                }

                string updateQuery = @"
                    UPDATE learning_videos 
                    SET skill_name = @skillName,
                        video_title = @videoTitle,
                        video_description = @videoDescription,
                        youtube_video_id = @youtubeVideoId,
                        duration_minutes = @durationMinutes,
                        thumbnail_url = @thumbnailUrl,
                        transcript = @transcript,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = @id";

                using MySqlCommand updateCmd = new(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@id", id);
                updateCmd.Parameters.AddWithValue("@skillName", request.SkillName);
                updateCmd.Parameters.AddWithValue("@videoTitle", request.VideoTitle);
                updateCmd.Parameters.AddWithValue("@videoDescription", request.VideoDescription ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@youtubeVideoId", request.YoutubeVideoId);
                updateCmd.Parameters.AddWithValue("@durationMinutes", request.DurationMinutes);
                updateCmd.Parameters.AddWithValue("@thumbnailUrl", request.ThumbnailUrl ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@transcript", request.Transcript ?? (object)DBNull.Value);

                int rowsAffected = updateCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"✅ Video updated: ID {id}");
                    return Ok(new { 
                        success = true, 
                        message = "Video updated successfully"
                    });
                }

                return StatusCode(500, new { error = "Failed to update video" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating video: {ex.Message}");
                return StatusCode(500, new { error = "Failed to update video", details = ex.Message });
            }
        }

        // DELETE /api/learningvideos/{id}
        // Delete learning video (Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteVideo(int id)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if video exists
                string checkQuery = "SELECT skill_name FROM learning_videos WHERE id = @id";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                
                var skillName = checkCmd.ExecuteScalar()?.ToString();
                if (skillName == null)
                {
                    return NotFound(new { error = $"Video with ID {id} not found" });
                }

                string deleteQuery = "DELETE FROM learning_videos WHERE id = @id";
                using MySqlCommand deleteCmd = new(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = deleteCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine($"✅ Video deleted: {skillName} (ID: {id})");
                    return Ok(new { 
                        success = true, 
                        message = $"Video '{skillName}' deleted successfully"
                    });
                }

                return StatusCode(500, new { error = "Failed to delete video" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting video: {ex.Message}");
                return StatusCode(500, new { error = "Failed to delete video", details = ex.Message });
            }
        }
    }

    // Request models
    public class TranscriptUpdateRequest
    {
        public string Transcript { get; set; } = string.Empty;
    }

    public class VideoCreateRequest
    {
        public string SkillName { get; set; } = string.Empty;
        public string VideoTitle { get; set; } = string.Empty;
        public string? VideoDescription { get; set; }
        public string YoutubeVideoId { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Transcript { get; set; }
    }

    public class VideoUpdateRequest
    {
        public string SkillName { get; set; } = string.Empty;
        public string VideoTitle { get; set; } = string.Empty;
        public string? VideoDescription { get; set; }
        public string YoutubeVideoId { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Transcript { get; set; }
    }
}
