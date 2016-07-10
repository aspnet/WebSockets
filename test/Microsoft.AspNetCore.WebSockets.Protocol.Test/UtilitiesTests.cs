// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;
using Xunit;

namespace Microsoft.AspNetCore.WebSockets.Protocol.Test
{
    public class UtilitiesTests
    {
        [Fact]
        public void MaskDataRoundTrips()
        {
            byte[] data = Encoding.UTF8.GetBytes("Hello World");
            byte[] orriginal = Encoding.UTF8.GetBytes("Hello World");
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Utilities.MaskInPlace(16843009, new ArraySegment<byte>(data));
            Assert.Equal(orriginal, data);
        }

        [Theory]
        [InlineData(0, 0, new byte[0])]
        [InlineData(1, 1, new byte[] { 0x75 })]
        [InlineData(2, 2, new byte[] { 0x75, 0x58 })]
        [InlineData(3, 3, new byte[] { 0x75, 0x58, 0xEB })]
        [InlineData(4, 0, new byte[] { 0x75, 0x58, 0xEB, 0xFF })]
        [InlineData(5, 1, new byte[] { 0x75, 0x58, 0xEB, 0xFF, 0x73 })]
        public void MaskInPlace(int bufferLength, int expectedMaskOffset, byte[] expectedOutput)
        {
            var random = new Random(1);
            var buffer = new byte[bufferLength];
            random.NextBytes(buffer);

            const int mask = 864578941;         // This value has each of the four mask bytes different, so it makes bugs more visible
            int maskOffset = 0;
            Utilities.MaskInPlace(mask, ref maskOffset, new ArraySegment<byte>(buffer));

            Assert.Equal(expectedMaskOffset, maskOffset);
            Assert.Equal(expectedOutput, buffer);
        }
    }
}
