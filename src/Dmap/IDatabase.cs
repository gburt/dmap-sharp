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
using System.Linq;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Dmap
{
    public interface IDatabase<P, T>
        where P : IPlaylist<T>
        where T : ITrack
    {
        int Id { get; }
        string Name { get; }

        int TrackCount { get; }
        IList<T> Tracks { get; }
        T LookupTrackById (int id);

        int PlaylistCount { get; }
        IList<P> Playlists { get; }
        P LookupPlaylistById (int id);
    }
}
