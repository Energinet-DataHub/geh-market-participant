﻿// Copyright 2020 Energinet DataHub A/S
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

using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.WebApi.Controllers;

[ApiController]
public sealed class PatchCertsController : ControllerBase
{
    private readonly ICertificateService _certificateService;

    public PatchCertsController(ICertificateService certificateService)
    {
        _certificateService = certificateService;
    }

    [HttpGet]
    [AllowAnonymous]
    [Route("patch-certs")]
    public async Task GetAsync()
    {
        await _certificateService.SyncWorkaroundAsync().ConfigureAwait(false);
    }
}