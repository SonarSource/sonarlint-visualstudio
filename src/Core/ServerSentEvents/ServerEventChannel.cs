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

using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using SonarQube.Client.Models.ServerSentEvents.ClientContract;

namespace SonarLint.VisualStudio.Core.ServerSentEvents
{
    /// <summary>
    /// Base class for channels of server events divided by topics
    /// </summary>
    /// <remarks>
    /// Even though <see cref="IServerSentEventSource{T}"/> and <see cref="IServerSentEventSourcePublisher{T}"/> do not allow their own methods to be called from different threads, this class was designed to allow the calls to two different interfaces' methods to be made from different threads at the same time.
    /// This means that calling from one thread <see cref="GetNextEventOrNullAsync"/> and calling <see cref="Publish"/> or <see cref="Dispose"/> from a different thread at the same time is allowed, while calling methods of the same interface from different threads is not.
    /// NOTE: there's an exception to this rule that was made to simplify the implementation of the events pump that permits calling Dispose and Publish concurrently, but results in an exception. For more info see <see cref="IServerSentEventSourcePublisher{T}"/>
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class ServerEventChannel<T> : IServerSentEventSource<T>, IServerSentEventSourcePublisher<T> where T : class, IServerEvent
    {
        private bool disposed;
        /// <summary>
        /// Channel that is used for storing and awaiting new items
        /// </summary>
        private readonly Channel<T> channel = Channel.CreateUnbounded<T>(new UnboundedChannelOptions{SingleReader = true, SingleWriter = true});

        public async Task<T> GetNextEventOrNullAsync()
        {
            await channel.Reader.WaitToReadAsync().ConfigureAwait(false);
            return channel.Reader.TryRead(out var item) 
                ? item 
                : null;
        }

        public void Publish(T serverEvent)
        {
            // The behaviour for TryWrite depends, in some aspects, on the type of the channel we use.In particular, what we are interested in is what it returns before and after marking the channel as complete.
            // Because we specifically create an unbound channel with the SingleReader = true, SingleWriter = true settings, we actually get a SingleConsumerUnboundedChannel instance that has the following behaviour: TryWrite always returns true before the channel is marked as complete, and always returns false afterwards.
            // This behaviour allows us to be sure that the only way we can interpret the false result is that the channel was closed.
            // This is valuable for us because the only other way of knowing that would be calling WriteAsync and catching the exception, as the ChannelReader.Completed task is only resolved when there's no more items in the channel, and that may not happen at the same time as we called ChannelWriter.Complete.
            // So in short, the reason for this assumption was to simplify our channel wrapper code.
            if (!channel.Writer.TryWrite(serverEvent ?? throw new ArgumentNullException(nameof(serverEvent))))
            {
                throw new ObjectDisposedException(nameof(ServerEventChannel<T>));
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            channel.Writer.Complete();
            disposed = true;
        }
    }
}
