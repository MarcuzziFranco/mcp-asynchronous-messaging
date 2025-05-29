using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpClient.Helper;

public static class SanitizeJson
{
    public static string Sanitize(string input)
    {
        Console.WriteLine("========================== SanitizeJson ==========================");
        Console.WriteLine($"INPUT RECIBIDO: {input}");
        Console.WriteLine("===============================================================");

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("ERROR: Input es nulo o vacío");
            return "{}";
        }

        try
        {
            // Paso 1: Limpiar marcadores de markdown comunes
            string cleaned = input.Replace("```json", "").Replace("```", "").Trim();
            Console.WriteLine($"DESPUÉS DE LIMPIAR MARKDOWN: {cleaned}");

            // Paso 2: Buscar JSON usando regex - objetos que empiecen con {
            string jsonPattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
            var matches = Regex.Matches(cleaned, jsonPattern, RegexOptions.Singleline);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string candidateJson = match.Value.Trim();
                    Console.WriteLine($"CANDIDATO JSON ENCONTRADO: {candidateJson}");
                    
                    if (TryParseJson(candidateJson, out string validJson))
                    {
                        Console.WriteLine($"JSON VÁLIDO EXTRAÍDO: {validJson}");
                        Console.WriteLine("========================== FIN SanitizeJson ==========================");
                        return validJson;
                    }
                }
            }

            // Paso 3: Buscar arrays JSON que empiecen con [
            string arrayPattern = @"\[(?:[^\[\]]|(?<open>\[)|(?<-open>\]))+(?(open)(?!))\]";
            var arrayMatches = Regex.Matches(cleaned, arrayPattern, RegexOptions.Singleline);

            if (arrayMatches.Count > 0)
            {
                foreach (Match match in arrayMatches)
                {
                    string candidateJson = match.Value.Trim();
                    Console.WriteLine($"CANDIDATO ARRAY JSON ENCONTRADO: {candidateJson}");
                    
                    if (TryParseJson(candidateJson, out string validJson))
                    {
                        Console.WriteLine($"ARRAY JSON VÁLIDO EXTRAÍDO: {validJson}");
                        Console.WriteLine("========================== FIN SanitizeJson ==========================");
                        return validJson;
                    }
                }
            }

            // Paso 4: Intentar parsear el string completo limpio
            Console.WriteLine("INTENTANDO PARSEAR STRING COMPLETO LIMPIO...");
            if (TryParseJson(cleaned, out string finalJson))
            {
                Console.WriteLine($"STRING COMPLETO ES JSON VÁLIDO: {finalJson}");
                Console.WriteLine("========================== FIN SanitizeJson ==========================");
                return finalJson;
            }

            // Paso 5: Si nada funciona, intentar encontrar cualquier estructura similar a JSON
            Console.WriteLine("BUSCANDO CUALQUIER ESTRUCTURA SIMILAR A JSON...");
            int startIndex = cleaned.IndexOfAny(new char[] { '{', '[' });
            if (startIndex >= 0)
            {
                for (int endIndex = cleaned.Length - 1; endIndex > startIndex; endIndex--)
                {
                    if (cleaned[endIndex] == '}' || cleaned[endIndex] == ']')
                    {
                        string substring = cleaned.Substring(startIndex, endIndex - startIndex + 1);
                        Console.WriteLine($"PROBANDO SUBSTRING: {substring}");
                        
                        if (TryParseJson(substring, out string substringJson))
                        {
                            Console.WriteLine($"SUBSTRING JSON VÁLIDO: {substringJson}");
                            Console.WriteLine("========================== FIN SanitizeJson ==========================");
                            return substringJson;
                        }
                    }
                }
            }

            Console.WriteLine("NO SE PUDO EXTRAER JSON VÁLIDO, RETORNANDO JSON VACÍO");
            Console.WriteLine("========================== FIN SanitizeJson ==========================");
            return "{}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR EN SanitizeJson: {ex.Message}");
            Console.WriteLine("========================== FIN SanitizeJson ==========================");
            return "{}";
        }
    }

    private static bool TryParseJson(string jsonString, out string validJson)
    {
        validJson = string.Empty;
        
        if (string.IsNullOrWhiteSpace(jsonString))
            return false;

        try
        {
            var doc = JsonDocument.Parse(jsonString);
            validJson = doc.RootElement.ToString();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

