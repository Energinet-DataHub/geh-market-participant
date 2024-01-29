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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Email;

namespace Energinet.DataHub.MarketParticipant.Application.Services.Email;

public sealed class EmailContentGenerator : IEmailContentGenerator
{
    public async Task<GeneratedEmail> GenerateAsync(EmailTemplate emailTemplate, IReadOnlyDictionary<string, string> additionalParameters)
    {
        ArgumentNullException.ThrowIfNull(emailTemplate);
        ArgumentNullException.ThrowIfNull(additionalParameters);

        var htmlTemplate = await GetTemplateAsync(emailTemplate.TemplateId).ConfigureAwait(false);
        var composite = emailTemplate.TemplateParameters.Concat(additionalParameters);

        var emailContent = InstantiateEmailContentFromTemplate(htmlTemplate, composite);
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

    private static string InstantiateEmailContentFromTemplate(string htmlTemplate, IEnumerable<KeyValuePair<string, string>> parameters)
    {
        foreach (var parameter in parameters)
        {
            htmlTemplate = htmlTemplate.Replace($"{{{parameter.Key}}}", parameter.Value, StringComparison.OrdinalIgnoreCase);
        }

        return htmlTemplate;
    }

    private static async Task<string> GetTemplateAsync(EmailTemplateId emailTemplateId)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Energinet.DataHub.MarketParticipant.Application.Services.Email.Templates.{emailTemplateId}.html";

        var templateStream = assembly.GetManifestResourceStream(resourceName);
        if (templateStream == null)
            throw new InvalidOperationException($"{nameof(EmailContentGenerator)} could not find template {emailTemplateId}.");

        await using (templateStream.ConfigureAwait(false))
        {
            using (var reader = new StreamReader(templateStream))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
