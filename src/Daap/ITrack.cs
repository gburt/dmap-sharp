/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;
using System.Collections;

namespace Daap
{
    public interface ITrack
    {
        string Artist { get; }
        string Album { get; }
        string Title { get; }
        string Genre { get; }
        int Year { get; }
        string Format { get; }
        TimeSpan Duration { get; }
        int Id { get; }
        int Size { get; }
        int TrackNumber { get; }
        int TrackCount { get; }
        int DiscNumber { get; }
        int DiscCount { get; }
        string FileName { get; }
        DateTime DateAdded { get; }
        DateTime DateModified { get; }
        short BitRate { get; }
    }
}
