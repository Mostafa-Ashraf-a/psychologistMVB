using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using MentalHealthAssessment.Application.Dtos;
using MentalHealthAssessment.Application.Interfaces;

namespace MentalHealthAssessment.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly FirestoreDb _firestoreDb;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public AuthService(IConfiguration configuration, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Firebase:ApiKey"] ?? "";
            var projectId = configuration["Firebase:ProjectId"] ?? "mentalhealth-ee5f9";
            var credentialPath = configuration["Firebase:CredentialFile"] ?? "mentalhealth-ee5f9-firebase-adminsdk-fbsvc-3ad23b3fcd.json";

            // Initialize Firebase Admin SDK (Singleton)
            if (FirebaseApp.DefaultInstance == null)
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
            }

            // Initialize Firestore connection
            var builder = new FirestoreDbBuilder
            {
                ProjectId = projectId,
                CredentialsPath = credentialPath
            };
            _firestoreDb = builder.Build();
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            // Standardize Role
            var role = request.Role;
            if (role != "Admin" && role != "Consultant" && role != "Patient")
            {
                throw new ArgumentException("Invalid Role. Must be 'Admin', 'Consultant', or 'Patient'.");
            }

            // Standardize Phone for Saudi Arabia
            var phone = request.PhoneNumber.Trim();
            var formattedPhone = phone;
            if (!formattedPhone.StartsWith("+"))
            {
                // Strip leading zero if present and prefix with +966
                var stripped = formattedPhone.StartsWith("0") ? formattedPhone.Substring(1) : formattedPhone;
                formattedPhone = $"+966{stripped}";
            }

            // In Firebase, we will use a pseudo-email based on the phone number
            var email = $"{phone}@psychologist.mvp";

            try
            {
                // Create user in Firebase Auth
                var userArgs = new UserRecordArgs
                {
                    Email = email,
                    Password = request.Password,
                    DisplayName = request.Username,
                    PhoneNumber = formattedPhone,
                    EmailVerified = true
                };

                var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userArgs);

                // Set Custom Claims for Roles
                var claims = new Dictionary<string, object>
                {
                    { "role", role }
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(userRecord.Uid, claims);

                // Save extended profile in Firestore
                var userRef = _firestoreDb.Collection("users").Document(userRecord.Uid);
                var userData = new Dictionary<string, object>
                {
                    { "uid", userRecord.Uid },
                    { "username", request.Username },
                    { "phoneNumber", phone },
                    { "role", role },
                    { "createdAt", DateTime.UtcNow },
                    { "createdBy", "Registration-System" }
                };
                await userRef.SetAsync(userData);

                // Perform login to get token immediately for the newly registered user
                var token = await GetFirebaseIdTokenAsync(email, request.Password);

                return new AuthResponseDto
                {
                    Uid = userRecord.Uid,
                    Username = request.Username,
                    PhoneNumber = phone,
                    Role = role,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Registration failed: {ex.Message}");
            }
        }

        public async Task<AuthResponseDto> LoginAsync(string usernameOrPhone, string password)
        {
            string email = "";
            string phone = "";
            string username = "";
            string uid = "";
            string role = "";

            // Check if input is a phone number (e.g. only digits or starts with +)
            bool isPhone = true;
            foreach (char c in usernameOrPhone)
            {
                if (!char.IsDigit(c) && c != '+' && c != '-')
                {
                    isPhone = false;
                    break;
                }
            }

            if (isPhone)
            {
                phone = usernameOrPhone;
                email = $"{phone}@psychologist.mvp";

                // Query Firestore to get username and role
                var usersQuery = _firestoreDb.Collection("users").WhereEqualTo("phoneNumber", phone);
                var querySnapshot = await usersQuery.GetSnapshotAsync();
                
                if (querySnapshot.Documents.Count > 0)
                {
                    var userDoc = querySnapshot.Documents[0];
                    uid = userDoc.GetValue<string>("uid");
                    username = userDoc.GetValue<string>("username");
                    role = userDoc.GetValue<string>("role");
                }
            }
            else
            {
                // Query Firestore to find the phone number associated with this username
                var usersQuery = _firestoreDb.Collection("users").WhereEqualTo("username", usernameOrPhone);
                var querySnapshot = await usersQuery.GetSnapshotAsync();

                if (querySnapshot.Documents.Count == 0)
                {
                    throw new Exception("User not found.");
                }

                var userDoc = querySnapshot.Documents[0];
                uid = userDoc.GetValue<string>("uid");
                username = userDoc.GetValue<string>("username");
                phone = userDoc.GetValue<string>("phoneNumber");
                role = userDoc.GetValue<string>("role");
                email = $"{phone}@psychologist.mvp";
            }

            try
            {
                // Get ID token from Firebase REST API
                var token = await GetFirebaseIdTokenAsync(email, password);

                return new AuthResponseDto
                {
                    Uid = uid,
                    Username = username,
                    PhoneNumber = phone,
                    Role = role,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Authentication failed: {ex.Message}");
            }
        }

        private async Task<string> GetFirebaseIdTokenAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";

            var requestBody = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorString = await response.Content.ReadAsStringAsync();
                throw new Exception($"Invalid credentials. Firebase details: {errorString}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            
            return doc.RootElement.GetProperty("idToken").GetString();
        }
    }
}
