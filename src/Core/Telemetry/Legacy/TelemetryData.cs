/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.ComponentModel;
using System.Globalization;
using System.Xml.Serialization;

namespace SonarLint.VisualStudio.Core.Telemetry.Legacy;

public sealed class TelemetryData
{
    public bool IsAnonymousDataShared { get; set; }

    public long NumberOfDaysOfUse { get; set; }

    [XmlIgnore] public DateTimeOffset InstallationDate { get; set; }

    [XmlElement(nameof(InstallationDate)), EditorBrowsable(EditorBrowsableState.Never)]
    public string InstallationDateString
    {
        get => InstallationDate.ToString("o");
        set => InstallationDate = ParseSavedString(value);
    }

    private static DateTimeOffset ParseSavedString(string data)
    {
        // ParseExact will throw an exception when value is invalid date, but
        // XmlSerializer will swallow it and return default(TelemetryData)
        return DateTimeOffset.ParseExact(data, "o", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal);
    }
}
