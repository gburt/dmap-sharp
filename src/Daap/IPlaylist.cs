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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Daap
{
    public interface IPlaylist<T> where T : ITrack
    {
        int Id { get; }
        string Name { get; }
        int TrackCount { get; }

        bool IsBasePlaylist { get; }

        int GetContainerId (int index);

        T this [int index] { get; }
        IEnumerable<T> Tracks { get; }
    }
}
