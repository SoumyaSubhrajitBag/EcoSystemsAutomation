using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace EcoSystemsAutomation.XMLHelper
{
    public class XMLDeserializer : IXMLDeserializer
    {
        public List<string> ReadXML(List<string> xmlPaths)
        {
            if (xmlPaths == null || !xmlPaths.Any())
            {
                throw new ArgumentException("No XML paths provided.", nameof(xmlPaths));
            }

            var xmlContents = new List<string>();
            var errors = new List<string>();

            // Log the total number of paths received
            Console.WriteLine($"Attempting to read {xmlPaths.Count} XML files");

            foreach (var path in xmlPaths)
            {
                try
                {
                    // Log each path being checked
                    Console.WriteLine($"Checking path: {path}");

                    // Check if path is valid
                    if (string.IsNullOrEmpty(path))
                    {
                        errors.Add("Path is null or empty");
                        continue;
                    }

                    // Get directory info for logging
                    var directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory))
                    {
                        errors.Add($"Directory does not exist: {directory}");
                        continue;
                    }

                    // Check file existence
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Found file at: {path}");
                        string content = File.ReadAllText(path);

                        // Verify the content is not empty
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            xmlContents.Add(content);
                            Console.WriteLine($"Successfully read content from: {path}");
                        }
                        else
                        {
                            errors.Add($"File is empty: {path}");
                        }
                    }
                    else
                    {
                        errors.Add($"File does not exist: {path}");
                    }
                }
                catch (Exception ex)
                {
                    // Log the full exception details
                    errors.Add($"Error reading {path}: {ex.Message}\nStack Trace: {ex.StackTrace}");
                }
            }

            // Log summary
            Console.WriteLine($"Successfully read {xmlContents.Count} out of {xmlPaths.Count} files");

            if (!xmlContents.Any())
            {
                var errorMessage = $"None of the specified XML files exist or are accessible.\n" +
                                  $"Attempted paths: {string.Join("\n", xmlPaths)}\n" +
                                  $"Errors encountered:\n{string.Join("\n", errors)}";

                throw new FileNotFoundException(errorMessage);
            }

            return xmlContents;
        }


        public void DeserializeXML(string xmlString, object reportObj)
        {
            if (string.IsNullOrWhiteSpace(xmlString))
            {
                throw new ArgumentException("The XML string cannot be null or empty.", nameof(xmlString));
            }
            if (reportObj == null)
            {
                throw new ArgumentNullException(nameof(reportObj), "The object to populate cannot be null.");
            }

            // Preprocess XML to convert date formats to ISO 8601
            xmlString = PreprocessXmlDates(xmlString);

            Type objectType = reportObj.GetType();
            var serializer = new XmlSerializer(objectType);

            try
            {
                using (var reader = new StringReader(xmlString))
                using (var xmlReader = XmlReader.Create(reader))
                {
                    var deserializedObject = serializer.Deserialize(xmlReader);
                    if (deserializedObject == null)
                    {
                        throw new InvalidOperationException("Deserialization resulted in a null object.");
                    }

                    CopyProperties(deserializedObject, reportObj);
                }
            }
            catch (InvalidOperationException ex) when (ex.InnerException is FormatException)
            {
                throw new FormatException(
                    "Failed to parse date in XML. Supported formats: " +
                    "M/d/yyyy h:mm:ss tt, MM/dd/yyyy HH:mm:ss, " +
                    "yyyy-MM-ddTHH:mm:ss, yyyy-MM-dd HH:mm:ss", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("An error occurred during deserialization.", ex);
            }
        }

        private void CopyProperties(object source, object target)
        {
            Type objectType = target.GetType();
            foreach (var property in objectType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.CanWrite)
                {
                    try
                    {
                        var value = property.GetValue(source);

                        if (property.PropertyType == typeof(DateTime) && value is string dateStr)
                        {
                            property.SetValue(target, ParseDateTime(dateStr));
                        }
                        else
                        {
                            property.SetValue(target, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Error setting property {property.Name}: {ex.Message}", ex);
                    }
                }
            }
        }

        private DateTime ParseDateTime(string dateStr)
        {
            var supportedFormats = new[]
            {
        "M/d/yyyy h:mm:ss tt",
        "MM/dd/yyyy HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-dd HH:mm:ss"
    };

            if (DateTime.TryParseExact(
                dateStr,
                supportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate))
            {
                return parsedDate;
            }

            throw new FormatException($"Date string '{dateStr}' does not match supported formats.");
        }

        private string PreprocessXmlDates(string xml)
        {
            var dateFormats = new[] { "M/d/yyyy h:mm:ss tt", "MM/dd/yyyy HH:mm:ss" };
            string pattern = "\\d{1,2}/\\d{1,2}/\\d{4} \\d{1,2}:\\d{2}:\\d{2} (AM|PM)";

            return Regex.Replace(xml, pattern, match =>
            {
                if (DateTime.TryParseExact(match.Value, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    return date.ToString("yyyy-MM-ddTHH:mm:ss");
                }
                return match.Value; // If parsing fails, leave the value unchanged
            });
        }


    }
}
