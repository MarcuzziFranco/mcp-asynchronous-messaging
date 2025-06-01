using System.Text.Json;
using System.Text.RegularExpressions;

namespace McpClient.Helper;

public static class SanitizeJson
{
    public static string Sanitize(string input)
    {
        //Console.WriteLine("========================== SanitizeJson ==========================");
        //Console.WriteLine($"INPUT RECIBIDO: {input}");
        //Console.WriteLine("===============================================================");

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("ERROR: Input es nulo o vacío");
            return "{}";
        }

        try
        {
            // Paso 1: Limpiar marcadores de markdown comunes
            string cleaned = input.Replace("```json", "").Replace("```", "").Trim();
            //Console.WriteLine($"DESPUÉS DE LIMPIAR MARKDOWN: {cleaned}");

            // Paso 2: Eliminar comentarios // al final de líneas
            string withoutComments = RemoveInlineComments(cleaned);
            //Console.WriteLine($"DESPUÉS DE ELIMINAR COMENTARIOS: {withoutComments}");

            // Paso 3: Normalizar comillas simples a comillas dobles para JSON válido
            string normalizedQuotes = NormalizeQuotes(withoutComments);
            //Console.WriteLine($"DESPUÉS DE NORMALIZAR COMILLAS: {normalizedQuotes}");

            // Paso 4: Buscar JSON usando regex - objetos que empiecen con {
            string jsonPattern = @"\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\}";
            var matches = Regex.Matches(normalizedQuotes, jsonPattern, RegexOptions.Singleline);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    string candidateJson = match.Value.Trim();
                    //Console.WriteLine($"CANDIDATO JSON ENCONTRADO: {candidateJson}");
                    
                    if (TryParseJson(candidateJson, out string validJson))
                    {
                        //Console.WriteLine($"JSON VÁLIDO EXTRAÍDO: {validJson}");
                        //Console.WriteLine("========================== FIN SanitizeJson ==========================");
                        return validJson;
                    }
                }
            }

            // Paso 5: Buscar arrays JSON que empiecen con [
            string arrayPattern = @"\[(?:[^\[\]]|(?<open>\[)|(?<-open>\]))+(?(open)(?!))\]";
            var arrayMatches = Regex.Matches(normalizedQuotes, arrayPattern, RegexOptions.Singleline);

            if (arrayMatches.Count > 0)
            {
                foreach (Match match in arrayMatches)
                {
                    string candidateJson = match.Value.Trim();
                    //Console.WriteLine($"CANDIDATO ARRAY JSON ENCONTRADO: {candidateJson}");
                    
                    if (TryParseJson(candidateJson, out string validJson))
                    {
                        //Console.WriteLine($"ARRAY JSON VÁLIDO EXTRAÍDO: {validJson}");
                        //Console.WriteLine("========================== FIN SanitizeJson ==========================");
                        return validJson;
                    }
                }
            }

            // Paso 6: Intentar parsear el string completo limpio
            //Console.WriteLine("INTENTANDO PARSEAR STRING COMPLETO LIMPIO...");
            if (TryParseJson(normalizedQuotes, out string finalJson))
            {
                //Console.WriteLine($"STRING COMPLETO ES JSON VÁLIDO: {finalJson}");
                //Console.WriteLine("========================== FIN SanitizeJson ==========================");
                return finalJson;
            }

            // Paso 7: Si nada funciona, intentar encontrar cualquier estructura similar a JSON
            //Console.WriteLine("BUSCANDO CUALQUIER ESTRUCTURA SIMILAR A JSON...");
            int startIndex = normalizedQuotes.IndexOfAny(new char[] { '{', '[' });
            if (startIndex >= 0)
            {
                for (int endIndex = normalizedQuotes.Length - 1; endIndex > startIndex; endIndex--)
                {
                    if (normalizedQuotes[endIndex] == '}' || normalizedQuotes[endIndex] == ']')
                    {
                        string substring = normalizedQuotes.Substring(startIndex, endIndex - startIndex + 1);
                        //Console.WriteLine($"PROBANDO SUBSTRING: {substring}");
                        
                        if (TryParseJson(substring, out string substringJson))
                        {
                            //Console.WriteLine($"SUBSTRING JSON VÁLIDO: {substringJson}");
                            //Console.WriteLine("========================== FIN SanitizeJson ==========================");
                            return substringJson;
                        }
                    }
                }
            }

            //Console.WriteLine("NO SE PUDO EXTRAER JSON VÁLIDO, RETORNANDO JSON VACÍO");
            //Console.WriteLine("========================== FIN SanitizeJson ==========================");
            return "{}";
        }
        catch (Exception ex)
        {
            //Console.WriteLine($"ERROR EN SanitizeJson: {ex.Message}");
            //Console.WriteLine("========================== FIN SanitizeJson ==========================");
            return "{}";
        }
    }

    private static string RemoveInlineComments(string input)
    {
        //Console.WriteLine("--- Iniciando eliminación de comentarios inline ---");
        
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        var cleanedLines = new List<string>();

        foreach (var line in lines)
        {
            string cleanedLine = RemoveCommentFromLine(line);
            cleanedLines.Add(cleanedLine);
            
            if (line != cleanedLine)
            {
                //Console.WriteLine($"LÍNEA ORIGINAL: {line}");
                //Console.WriteLine($"LÍNEA LIMPIA: {cleanedLine}");
            }
        }

        var result = string.Join("\n", cleanedLines);
        //Console.WriteLine("--- Fin eliminación de comentarios inline ---");
        return result;
    }

    private static string RemoveCommentFromLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return line;

        bool inString = false;
        bool escaped = false;
        
        for (int i = 0; i < line.Length - 1; i++)
        {
            char current = line[i];
            char next = line[i + 1];

            // Si estamos en una cadena, solo nos importa el escape y las comillas
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                    continue;
                }
                
                if (current == '\\')
                {
                    escaped = true;
                    continue;
                }
                
                if (current == '"')
                {
                    inString = false;
                }
                continue;
            }

            // Si no estamos en una cadena
            if (current == '"')
            {
                inString = true;
                continue;
            }

            // Buscar comentarios // fuera de strings
            if (current == '/' && next == '/')
            {
                // Encontramos un comentario, retornar la línea hasta este punto (sin espacios al final)
                return line.Substring(0, i).TrimEnd();
            }
        }

        return line;
    }

    private static string NormalizeQuotes(string input)
    {
        //Console.WriteLine("--- Iniciando normalización de comillas ---");
        
        if (string.IsNullOrWhiteSpace(input))
            return input;

        bool inPropertyName = false;
        bool inPropertyValue = false;
        bool inDoubleQuotes = false;
        bool inSingleQuotes = false;
        bool escaped = false;
        var result = new System.Text.StringBuilder();

        for (int i = 0; i < input.Length; i++)
        {
            char current = input[i];
            char? next = i < input.Length - 1 ? input[i + 1] : null;

            // Handle escape sequences
            if (escaped)
            {
                result.Append(current);
                escaped = false;
                continue;
            }

            if (current == '\\')
            {
                result.Append(current);
                escaped = true;
                continue;
            }

            // Handle quotes
            if (current == '"')
            {
                if (inSingleQuotes)
                {
                    // Estamos dentro de comillas simples, esta " es literal
                    result.Append(current);
                }
                else
                {
                    // Manejo normal de comillas dobles
                    inDoubleQuotes = !inDoubleQuotes;
                    if (!inDoubleQuotes)
                    {
                        inPropertyName = false;
                        inPropertyValue = false;
                    }
                    result.Append(current);
                }
                continue;
            }

            if (current == '\'')
            {
                if (inDoubleQuotes)
                {
                    // Estamos dentro de comillas dobles, esta ' es literal
                    result.Append(current);
                }
                else
                {
                    // Convertir comilla simple a doble para JSON válido
                    //Console.WriteLine($"CONVIRTIENDO ' a \" en posición {i}");
                    inSingleQuotes = !inSingleQuotes;
                    if (!inSingleQuotes)
                    {
                        inPropertyValue = false;
                    }
                    result.Append('"'); // Convertir ' a "
                }
                continue;
            }

            // Otros caracteres
            result.Append(current);
        }

        string normalized = result.ToString();
        
        if (input != normalized)
        {
            //Console.WriteLine($"ORIGINAL: {input}");
            //Console.WriteLine($"NORMALIZADO: {normalized}");
        }
        
        //Console.WriteLine("--- Fin normalización de comillas ---");
        return normalized;
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

