// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.CountyService
{
    using Newtonsoft.Json;
    using System.Net.Http.Formatting;

    public static class FormatterConfig
    {
        public static void ConfigureFormatters(MediaTypeFormatterCollection formatters)
        {
            formatters.Remove(formatters.XmlFormatter);

            JsonSerializerSettings settings = formatters.JsonFormatter.SerializerSettings;
            settings.Formatting = Formatting.None;
        }
    }
}