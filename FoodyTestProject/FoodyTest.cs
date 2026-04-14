
using FoodyTestProject.Models;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Net;
using System.Reflection.Metadata;
using System.Text.Json;
using System.Text.Json.Serialization;




namespace FoodyTestProject
{

    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreateadFoodId;

        private const string BaseUrl = "http://144.91.123.158:81";
        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJlZDE2OTg5Ni01YWQ2LTQ0NTktOWJlZC1jMDk4MGRlYTEwODYiLCJpYXQiOiIwNC8xNi8yMDI2IDE2OjI3OjI4IiwiVXNlcklkIjoiY2RjNzY0ZjktOTdjYi00NzJjLTc0YmItMDhkZTc2OGU4Zjk3IiwiRW1haWwiOiJwbGFtaUtAc29mdHVuaS5jb20iLCJVc2VyTmFtZSI6IlBsYW1pdG9LIiwiZXhwIjoxNzc2Mzc4NDQ4LCJpc3MiOiJGb29keV9BcHBfU29mdFVuaSIsImF1ZCI6IkZvb2R5X1dlYkFQSV9Tb2Z0VW5pIn0.3X2vfAzI5fbPbUE6BNQfYXp8uOxZ1BYy5VY97B0HNzk";

        private const string LoginUsername = "PlamitoK";
        private const string LoginPassword = "parola123456";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginUsername, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("token").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }
        [Order(1)]
        [Test]
        public void CreateNewFood_WithRequiredFIelds_ShouldReturnSuccess()
        {
            var foodData = new FoodDTO
            {
                Name = "Test Food",
                Description = "This is a test food description.",
                Url = "https://example.com/food.jpg" // Увери се, че това не е задължително поле
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(foodData);

            var response = this.client.Execute(request);

            // 1. Провери дали статусът е 201 Created
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                $"Failed to create food. Status: {response.StatusCode}, Content: {response.Content}");

            // 2. Десериализирай отговора
            var readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Това игнорира разлики в малки/главни букви
            });

            // 3. ПРОВЕРИ дали FoodId не е null в самия обект
            Assert.That(readyResponse.FoodId, Is.Not.Null.And.Not.Empty, "API did not return a FoodId!");

            // Записваме ID-то за следващия тест
            lastCreateadFoodId = readyResponse.FoodId;
        }

        [Order(2)]
        [Test]
        public void EditFoodTitle_ShouldChangeTitle()
        {
            // 1. Увери се, че ID-то не е null (ако първият тест се е провалил)
            Assert.That(lastCreateadFoodId, Is.Not.Null.Or.Empty, "ID of the created food is missing.");

            RestRequest request = new RestRequest($"/api/Food/Edit/{lastCreateadFoodId}", Method.Patch);

            // Използвай AddJsonBody вместо AddBody за сигурност
            request.AddJsonBody(new[]
            {
        new
        {
            path = "/name",
            op = "replace",
            value = "Chicken Soup"
        }
    });

            var response = this.client.Execute(request);

            // 2. ПЪРВО провери статуса, после десериализирай
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected 200 OK but got {response.StatusCode}. Content: {response.Content}");

            // 3. Провери дали съдържанието не е празно преди десериализация
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "API returned an empty response body.");

            ApiResponseDTO editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));
            
        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturnSuccess()
        {
            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = this.client.Execute(request);

            var responseItems = JsonSerializer.Deserialize<List<ApiResponseDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItems, Is.Not.Empty);
            Assert.That(responseItems, Is.Not.Null);

        

        }
        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShouldSucceed()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{lastCreateadFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            //response.Content = { "msg": "Deleted successfully!" }
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            //readyResponse
            //Msg = "Deleted successfully!"
            //FoodId = null
            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }


        [Order(5)]
        [Test]
        public void CreateFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            FoodDTO food = new FoodDTO
            {
                Name = "",
                Description = ""
            };

            RestRequest request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddBody(food);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditTitleOfNonExistingFood_ShouldReturnNotFound()
        {
            string nonExistingFoodId = "12345";
            RestRequest request = new RestRequest($"/api/Food/Edit/{nonExistingFoodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Chicken Soup"
                }
            });
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            //response.Content = { "msg": "No food revues..." }
            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            //readyResponse
            //Msg = "No food revues..."
            //FoodId = null
            Assert.That(readyResponse.Msg, Is.EqualTo("No food revues..."));
        }
        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            // Генерираме случаен GUID
            string nonExistingFoodId = Guid.NewGuid().ToString();

            // Променяме начина на конструиране на заявката
            var request = new RestRequest("/api/Food/Delete", Method.Delete);
            request.AddQueryParameter("id", nonExistingFoodId); // Пробвай това, ако името на параметъра в API-то е "id"

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        [OneTimeTearDown]

        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
