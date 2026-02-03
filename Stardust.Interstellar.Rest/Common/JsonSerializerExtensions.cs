using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Stardust.Particles;

namespace Stardust.Interstellar.Rest.Common
{
    public static class JsonSerializerExtensions
    {
        // RFC 7230: token = 1*tchar
        // tchar = "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" / "-" / "." /
        //         "^" / "_" / "`" / "|" / "~" / DIGIT / ALPHA
        private static readonly Regex ValidHeaderNamePattern = new Regex(
            @"^[A-Za-z0-9!#$%&'*+\-.^_`|~]+$",
            RegexOptions.Compiled);

        /// <summary>
        /// Validates and sanitizes an HTTP header name according to RFC 7230.
        /// </summary>
        /// <param name="headerName">The header name to sanitize.</param>
        /// <returns>The sanitized header name.</returns>
        /// <exception cref="ArgumentException">Thrown when the header name is null, empty, or contains invalid characters.</exception>
        public static string SanitizeHttpHeaderName(this string headerName)
        {
            if (string.IsNullOrWhiteSpace(headerName))
                throw new ArgumentException("Header name cannot be null or whitespace.", nameof(headerName));

            // First, remove any injection characters (CR, LF, and encoded variants)
            var sanitized = headerName
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("%0d", string.Empty).Replace("%0D", string.Empty)
                .Replace("%0a", string.Empty).Replace("%0A", string.Empty)
                .Replace("%250d", string.Empty).Replace("%250D", string.Empty)
                .Replace("%250a", string.Empty).Replace("%250A", string.Empty)
                .Replace("\u2028", string.Empty)
                .Replace("\u2029", string.Empty)
                .Replace(":", string.Empty)
                .Replace(" ", string.Empty);

            if (string.IsNullOrWhiteSpace(sanitized))
                throw new ArgumentException("Header name contains only invalid characters.", nameof(headerName));

            // Validate against RFC 7230 token pattern
            if (!ValidHeaderNamePattern.IsMatch(sanitized))
                throw new ArgumentException(
                    string.Format("Header name '{0}' contains invalid characters. Valid characters are: A-Z, a-z, 0-9, and ! # $ % & ' * + - . ^ _ ` | ~", sanitized),
                    nameof(headerName));

            return sanitized;
        }

        /// <summary>
        /// Attempts to sanitize an HTTP header name, returning false if sanitization fails.
        /// </summary>
        /// <param name="headerName">The header name to sanitize.</param>
        /// <param name="sanitizedName">The sanitized header name, or null if sanitization failed.</param>
        /// <returns>True if sanitization succeeded; otherwise, false.</returns>
        public static bool TrySanitizeHttpHeaderName(this string headerName, out string sanitizedName)
        {
            sanitizedName = null;
            try
            {
                sanitizedName = headerName.SanitizeHttpHeaderName();
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Sanitizes an HTTP header value to prevent header injection attacks.
        /// </summary>
        /// <param name="header">The header value to sanitize.</param>
        /// <returns>The sanitized header value with CR, LF, and related characters removed.</returns>
        public static string SanitizeHttpHeaderValue(this string header)
        {
            if (string.IsNullOrWhiteSpace(header)) return header;

            return header
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("%0d", string.Empty).Replace("%0D", string.Empty)
                .Replace("%0a", string.Empty).Replace("%0A", string.Empty)
                .Replace("%250d", string.Empty).Replace("%250D", string.Empty)
                .Replace("%250a", string.Empty).Replace("%250A", string.Empty)
                .Replace("\u2028", string.Empty)
                .Replace("\u2029", string.Empty);
        }

        private static Dictionary<Type, JsonSerializerSettings> clientSerializerSettings = new Dictionary<Type, JsonSerializerSettings>();
        
        public static JsonSerializerSettings AddClientSerializer<T>(this JsonSerializerSettings settings) where T : class
        {
            if (clientSerializerSettings.ContainsKey(typeof(T))) throw new AmbiguousMatchException(string.Format("Serailization settings already configured for {0}", nameof(T)));
            clientSerializerSettings.Add(typeof(T), settings);
            return settings;
        }

        public static JsonSerializerSettings GetClientSerializationSettings<T>(this T service) where T : class
        {
            var serviceType = typeof(T);    
            return serviceType.GetClientSerializationSettings();
        }

        public static JsonSerializerSettings GetClientSerializationSettings(this Type serviceType)
        {
            JsonSerializerSettings settings;
            return clientSerializerSettings.TryGetValue(serviceType, out settings) ? settings : JsonConvert.DefaultSettings != null ? JsonConvert.DefaultSettings() : null;
        }

        public static void UseSerializerSettingsFor(Type messageOrServiceType, JsonSerializerSettings settings)
        {
            if (clientSerializerSettings.ContainsKey(messageOrServiceType)) throw new AmbiguousMatchException(string.Format("Serailization settings already configured for {0}", messageOrServiceType.Name));
            clientSerializerSettings.Add(messageOrServiceType, settings);
        }
    }
}
