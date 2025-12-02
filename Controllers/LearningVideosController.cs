using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class LearningVideosController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LearningVideosController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET /api/learningvideos
        [HttpGet]
        public IActionResult GetAllVideos()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, skill_name, video_title, video_description, 
                           youtube_video_id, duration_minutes, thumbnail_url
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
                            : reader.GetString("thumbnail_url")
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
    }
}
