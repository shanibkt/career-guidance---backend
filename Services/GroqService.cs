using System.Text;
using System.Text.Json;
using MyFirstApi.Models;

namespace MyFirstApi.Services
{
    public class GroqService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _model = "llama-3.1-8b-instant";
        private readonly string _chatModel = "llama-3.1-8b-instant"; // Changed from 70b to 8b for better reliability

        public GroqService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured");
            _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            
            Console.WriteLine($"🔧 GroqService initialized with chat model: {_chatModel}");
            Console.WriteLine($"🔧 API Key (first 20 chars): {_apiKey.Substring(0, Math.Min(20, _apiKey.Length))}...");
        }

        public async Task<string> GenerateQuizQuestions(string userProfile)
        {
            var prompt = $@"You are an expert career assessment specialist. Generate EXACTLY 10 skill-based questions for this user.

USER PROFILE:
{userProfile}

ANALYZE THE USER'S SKILLS AND CREATE QUESTIONS TO TEST EACH SKILL.

REQUIREMENTS:
1. ALL 10 questions MUST be ""multiple_choice"" type
2. Each question MUST test a SPECIFIC skill from the user's profile
3. Each question MUST include the ""skill_category"" field indicating which skill it tests
4. Each question MUST include the ""correct_answer"" field (A, B, C, or D)
5. Questions MUST be challenging and technical to properly assess skill level
6. Options should include 1 correct answer and 3 plausible distractors

EXAMPLE QUESTION FORMAT:
{{
  ""id"": 1,
  ""question"": ""In Flutter, which widget is used for creating scrollable lists?"",
  ""type"": ""multiple_choice"",
  ""skill_category"": ""Flutter"",
  ""correct_answer"": ""B"",
  ""options"": [
    ""A) Container"",
    ""B) ListView"",
    ""C) Column"",
    ""D) Stack""
  ]
}}

CRITICAL - YOU MUST FOLLOW THIS EXACT JSON FORMAT:
{{
  ""questions"": [
    {{""id"": 1, ""question"": ""[Technical question about skill 1]"", ""type"": ""multiple_choice"", ""skill_category"": ""[SkillName]"", ""correct_answer"": ""[A/B/C/D]"", ""options"": [""A) ..."", ""B) ..."", ""C) ..."", ""D) ...""]}},
    {{""id"": 2, ""question"": ""[Technical question about skill 2]"", ""type"": ""multiple_choice"", ""skill_category"": ""[SkillName]"", ""correct_answer"": ""[A/B/C/D]"", ""options"": [""A) ..."", ""B) ..."", ""C) ..."", ""D) ...""]}},
    ... continue for 10 questions total ...
  ]
}}

IMPORTANT:
- Each question MUST test actual technical knowledge of the skill
- Distribute questions across ALL user skills
- If user has fewer skills, create multiple questions per skill
- Make questions progressively harder (basic → intermediate → advanced)
- Ensure correct_answer matches one of the options (A, B, C, or D)

Return ONLY valid JSON. NO markdown formatting. NO explanations. JUST the JSON object.";

            return await CallGroqAPI(prompt, "You are a technical skills assessment expert. Generate 10 skill-based multiple-choice questions with correct answers and skill categories. Questions MUST be technical and specific to the user's skills.");
        }

        public async Task<string> GenerateCareerRecommendations(string userProfile, string quizAnswers, string careersData)
        {
            var prompt = $@"Analyze this user and provide career match percentages:

User Profile:
{userProfile}

Quiz Responses:
{quizAnswers}

Available Careers:
{careersData}

Task:
1. Analyze how well the user fits each career based on quiz responses, education, skills, and interests
2. Assign match percentage (0-100) for each career
3. Provide reasoning for matches
4. Identify user's strengths for each career
5. Suggest areas to develop

Return ONLY valid JSON, no markdown:
{{
  ""recommendations"": [
    {{
      ""career_id"": 5,
      ""match_percentage"": 92.5,
      ""reasoning"": ""Your strong problem-solving skills align perfectly..."",
      ""strengths"": [""Analytical thinking"", ""Technical aptitude""],
      ""areas_to_develop"": [""Team collaboration""]
    }}
  ]
}}

Return top 10 careers ranked by match percentage.";

            return await CallGroqAPI(prompt, "You are a career matching expert. Analyze user profiles and provide accurate career recommendations.", 0.5f);
        }

        private async Task<string> CallGroqAPI(string userPrompt, string systemPrompt, float temperature = 0.7f)
        {
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = temperature, // Lower temperature for more consistent formatting
                max_tokens = 2048,
                response_format = new { type = "json_object" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Set timeout for HTTP request (20 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            
            var response = await _httpClient.PostAsync("chat/completions", content, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);
            
            return result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }

        public async Task<string> GetChatResponse(string userMessage, List<ChatMessage>? history = null, string? userProfile = null)
        {
            try
            {
                Console.WriteLine("🔵 GetChatResponse called");
                Console.WriteLine($"🔵 User message: {userMessage}");
                Console.WriteLine($"🔵 Has profile: {!string.IsNullOrEmpty(userProfile)}");
                
                var messages = new List<object>();
                
                // Enhanced system prompt for personalized career guidance
                var systemPrompt = @"You are an expert AI Career Guidance Assistant. 

KEY RULES:
- Keep responses SHORT (2-3 sentences, max 50 words)
- Only use the user's name occasionally (not in every message)
- Reference user's SPECIFIC skills when relevant
- Give ONE actionable suggestion per response
- Be conversational and friendly
- Ask follow-up questions to keep conversation flowing";

                if (!string.IsNullOrEmpty(userProfile))
                {
                    systemPrompt += $"\n\n{userProfile}";
                    Console.WriteLine("✅ Profile context added to AI prompt");
                }
                else
                {
                    Console.WriteLine("⚠️ No profile context available");
                }

                messages.Add(new { role = "system", content = systemPrompt });

                // Add conversation history (last 6 messages for context)
                if (history != null && history.Any())
                {
                    foreach (var msg in history.OrderBy(m => m.Timestamp).TakeLast(6))
                    {
                        messages.Add(new { role = msg.Role, content = msg.Message });
                    }
                }

                // Add current user message
                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = _chatModel,
                    messages = messages,
                    temperature = 0.7,
                    max_tokens = 150,
                    top_p = 1.0
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"🔵 Calling Groq API with model: {_chatModel}");
                Console.WriteLine($"🔵 API URL: {_httpClient.BaseAddress}chat/completions");

                // Set timeout for chat (30 seconds)
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                
                var response = await _httpClient.PostAsync("chat/completions", content, cts.Token);
                
                Console.WriteLine($"🔵 Groq API Response Status: {response.StatusCode}");
                
                var responseJson = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🔵 Response (first 200 chars): {responseJson.Substring(0, Math.Min(200, responseJson.Length))}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"🔴 Groq API Error Response: {responseJson}");
                    throw new HttpRequestException($"Groq API returned {response.StatusCode}: {responseJson}");
                }

                var result = JsonSerializer.Deserialize<GroqChatResponse>(responseJson);
                
                var aiResponse = result?.Choices?.FirstOrDefault()?.Message?.Content 
                    ?? "I'm having trouble responding right now. Please try again.";

                Console.WriteLine($"🟢 AI Response: {aiResponse.Substring(0, Math.Min(100, aiResponse.Length))}...");
                
                return aiResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔴 GetChatResponse Exception: {ex.GetType().Name}");
                Console.WriteLine($"🔴 Error Message: {ex.Message}");
                Console.WriteLine($"🔴 Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> EnhanceProfessionalSummary(string currentSummary, string jobTitle, List<string> skills, List<string> experiences)
        {
            var skillsList = skills != null && skills.Count > 0 ? string.Join(", ", skills) : "various technical skills";
            var experiencesList = experiences != null && experiences.Count > 0 
                ? string.Join("; ", experiences.Take(3)) 
                : "professional experience";

            var prompt = $@"Write a professional 3-sentence resume summary for a {jobTitle}.

Skills: {skillsList}
Experience: {experiencesList}

Write ONLY 3 sentences. No questions, no greetings, just the summary.";

            var requestBody = new
            {
                model = _chatModel,
                messages = new[]
                {
                    new { role = "system", content = "You write professional resume summaries. Output ONLY the summary text." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 150,
                response_format = new { type = "text" }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            Console.WriteLine($"🔵 Sending request to Groq API...");
            Console.WriteLine($"🔵 Prompt: {prompt.Substring(0, Math.Min(200, prompt.Length))}...");
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
            var apiResponse = await _httpClient.PostAsync("chat/completions", content, cts.Token);
            
            Console.WriteLine($"🔵 Groq API Status: {apiResponse.StatusCode}");
            
            apiResponse.EnsureSuccessStatusCode();

            var responseJson = await apiResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"🔵 Groq Response JSON: {responseJson.Substring(0, Math.Min(500, responseJson.Length))}...");
            
            var result = JsonDocument.Parse(responseJson);
            var response = result.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            Console.WriteLine($"✅ Groq response (length: {response.Length}): {response}");
            
            if (string.IsNullOrWhiteSpace(response))
            {
                Console.WriteLine($"⚠️ WARNING: Groq returned empty response!");
                return "Unable to generate AI summary at this time.";
            }
            
            return response.Trim();
        }
    }
}

