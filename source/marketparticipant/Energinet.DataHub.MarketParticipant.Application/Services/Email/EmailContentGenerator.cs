// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketParticipant.Application.Services.Email;

public sealed class EmailContentGenerator : IEmailContentGenerator
{
    public async Task<GeneratedEmail> GenerateAsync(EmailTemplate emailTemplate, IReadOnlyDictionary<string, string> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var htmlTemplate = await GetTemplateAsync(emailTemplate).ConfigureAwait(false);

        var emailContent = InstantiateEmailContentFromTemplate(htmlTemplate, parameters);
        var emailSubject = ReadTitleFromHtml(emailContent);

        return new GeneratedEmail(emailSubject, emailContent);
    }

    private static string ReadTitleFromHtml(string emailContent)
    {
        var titleStart = emailContent.IndexOf("<title>", StringComparison.OrdinalIgnoreCase);
        var titleEnd = emailContent.IndexOf("</title>", StringComparison.OrdinalIgnoreCase);

        if (titleStart == -1 || titleEnd == -1)
            throw new InvalidOperationException("Could not find the title in email template.");

        return emailContent.Substring(titleStart + 7, titleEnd - titleStart - 7);
    }

    private static string InstantiateEmailContentFromTemplate(string htmlTemplate, IReadOnlyDictionary<string, string> parameters)
    {
        foreach (var parameter in parameters)
        {
            htmlTemplate = htmlTemplate.Replace($"{{{parameter.Key}}}", parameter.Value, StringComparison.OrdinalIgnoreCase);
        }

        return htmlTemplate;
    }

    private static async Task<string> GetTemplateAsync(EmailTemplate emailTemplate)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Energinet.DataHub.MarketParticipant.Application.Services.Email.Templates.{emailTemplate}.html";

        var templateStream = assembly.GetManifestResourceStream(resourceName);
        if (templateStream == null)
            throw new InvalidOperationException($"{nameof(EmailContentGenerator)} could not find template {emailTemplate}.");

        await using (templateStream.ConfigureAwait(false))
        {
            using (var reader = new StreamReader(templateStream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
