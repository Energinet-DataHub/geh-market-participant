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
using Energinet.DataHub.MarketParticipant.Authorization.Model;
using Energinet.DataHub.MarketParticipant.Authorization.Model.Parameters;
using Energinet.DataHub.MarketParticipant.Authorization.Services;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Authorization;

[UnitTest]
public sealed class SignatureRequestTests
{
    [Fact]
    public void SignatureRequest_ToArray_SingleParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 1));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 1));
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter2", "test"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_SameKey_SameTypes_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test2"));
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_SameKey_SameTypes_DifferentOrder_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromString("MeteringPointPeriod", "test2"));
        target.AddSignatureParameter(SignatureParameter.FromString("MeteringPointPeriod", "test"));

        var target2 = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target2.AddSignatureParameter(SignatureParameter.FromString("MeteringPointPeriod", "test"));
        target2.AddSignatureParameter(SignatureParameter.FromString("MeteringPointPeriod", "test2"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_DifferentOrder_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 1));
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter2", "test"));

        var target2 = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target2.AddSignatureParameter(SignatureParameter.FromString("TestParameter2", "test"));
        target2.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 1));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_DifferentMultipleParams_SameKey_DifferentValues_ProducesDifferentResults()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test"));

        var target2 = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target2.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test2"));
        target2.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.NotEqual(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleLongParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 1));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleStringParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromString("TestParameter", "test"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleEicFunctionParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromEicFunction("TestParameter", EicFunction.BalanceResponsibleParty));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_Add_SameKey_DifferentTypes_ThrowsException()
    {
        // Arrange
        var target = new SignatureRequest(DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeMilliseconds());
        target.AddSignatureParameter(SignatureParameter.FromLong("TestParameter", 123));

        // Act + Assert
        Assert.Throws<ArgumentException>(() => target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter")));
    }
}
