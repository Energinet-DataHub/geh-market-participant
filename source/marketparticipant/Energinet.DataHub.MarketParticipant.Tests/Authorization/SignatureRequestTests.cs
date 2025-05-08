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
using Energinet.DataHub.MarketParticipant.Authorization.Restriction;
using Energinet.DataHub.MarketParticipant.Authorization.Restriction.Parameters;
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
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromLong(1, "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromLong(1, "TestParameter"));
        target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter2"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_SameKey_SameTypes_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromString("test2", "TestParameter"));
        target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_SameKey_SameTypes_DifferentOrder_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromString("test2", "MeteringPointPeriod"));
        target.AddSignatureParameter(SignatureParameter.FromString("test", "MeteringPointPeriod"));

        var target2 = new SignatureRequest();
        target2.AddSignatureParameter(SignatureParameter.FromString("test", "MeteringPointPeriod"));
        target2.AddSignatureParameter(SignatureParameter.FromString("test2", "MeteringPointPeriod"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_MultipleParams_DifferentOrder_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromLong(1, "TestParameter"));
        target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter2"));

        var target2 = new SignatureRequest();
        target2.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter2"));
        target2.AddSignatureParameter(SignatureParameter.FromLong(1, "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_GetBytes_DifferentMultipleParams_SameKey_DifferentValues_ProducesDifferentResults()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter"));

        var target2 = new SignatureRequest();
        target2.AddSignatureParameter(SignatureParameter.FromString("test2", "TestParameter"));
        target2.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target2.CreateSignatureParamBytes();

        Assert.NotEqual(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleLongParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromLong(1, "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleStringParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleEicFunctionParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromEicFunction(EicFunction.BalanceResponsibleParty, "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_ToArray_SingleEnumParam_AlwaysProducesSameResult()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromEnum(EicFunction.BalanceResponsibleParty, "TestParameter"));

        // Act + Assert
        var expected = target.CreateSignatureParamBytes();
        var actual = target.CreateSignatureParamBytes();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SignatureRequest_Add_SameKey_DifferentTypes_ThrowsException()
    {
        // Arrange
        var target = new SignatureRequest();
        target.AddSignatureParameter(SignatureParameter.FromEnum(EicFunction.BalanceResponsibleParty, "TestParameter"));

        // Act + Assert
        Assert.Throws<ArgumentException>(() => target.AddSignatureParameter(SignatureParameter.FromString("test", "TestParameter")));
    }
}
