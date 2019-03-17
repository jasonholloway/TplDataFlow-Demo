using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Shouldly;
using Xunit;
using static Sandbox.DataFlowDsl;

namespace Sandbox 
{
    public class Examples 
    {
        [Fact]
        public async Task Style1() 
        {
            var sink = new BufferBlock<Tracked>();
            
            var trackingPoller = new ActionBlock<Trackable>(async job => 
            {
                var polled = await PollCarrier(job.CarrierRef);

                var mapped = await polled
                                    .Select(raw => MapRaw(job, raw))
                                    .WhenAll();

                foreach (var m in mapped) sink.Post(m);
            });
            
            var jobPoller = new ActionBlock<Tick>(tick => 
            {
                //...
                trackingPoller.Post(new Trackable("wibble123")); 
            });

            
            jobPoller.Post(new Tick(DateTime.MinValue));
            await Task.Delay(30);

            
            var trackeds = sink.GatherAll();
            trackeds.Length.ShouldBe(2);
            trackeds.ShouldContain(t => t.Info == "Hello!");
            trackeds.ShouldContain(t => t.Info == "Boo!");
        }
        
        [Fact]
        public async Task Style2() 
        {
            var jobPoller = new TransformManyBlock<Tick, Trackable>(
                tick => new[] { new Trackable("wibble123" )});
            
            var trackingPoller = new TransformManyBlock<Trackable, Tracked>(
                async job => 
            {
                var polled = await PollCarrier(job.CarrierRef);
                return await polled
                        .Select(tracked => MapRaw(job, tracked))
                        .WhenAll();
            });
            
            var sink = new BufferBlock<Tracked>();

            LinkUp(jobPoller, trackingPoller);
            LinkUp(trackingPoller, sink);
            
            
            jobPoller.Post(new Tick(DateTime.MinValue));
            jobPoller.Complete();
            await trackingPoller.Completion;
            
            
            var trackeds = sink.GatherAll();
            trackeds.Length.ShouldBe(2);
            trackeds.ShouldContain(t => t.Info == "Hello!");
            trackeds.ShouldContain(t => t.Info == "Boo!");
        }
        
        [Fact]
        public async Task Style2_FurtherSplit() 
        {
            var jobPoller = new TransformManyBlock<Tick, Trackable>(
                tick => new[] { new Trackable("wibble123" )});

            var trackingPoller = new TransformManyBlock<Trackable, (Trackable, RawTracking)>(
                async job => {
                    var polled = await PollCarrier(job.CarrierRef);
                    return polled.Select(raw => (job, raw));
                });
            
            var trackingMapper = new TransformBlock<(Trackable, RawTracking), Tracked>(
                tup => {
                    var (trackable, raw) = tup;
                    return MapRaw(trackable, raw);
                });
            
            var sink = new BufferBlock<Tracked>();

            LinkUp(jobPoller, trackingPoller);
            LinkUp(trackingPoller, trackingMapper);
            LinkUp(trackingMapper, sink);
            
            jobPoller.Post(new Tick(DateTime.MinValue));
            jobPoller.Complete();
            await trackingMapper.Completion;
            
            var trackeds = sink.GatherAll();
            trackeds.Length.ShouldBe(2);
            trackeds.ShouldContain(t => t.Info == "Hello!");
            trackeds.ShouldContain(t => t.Info == "Boo!");
        }
        
        [Fact]
        public async Task Style2_FancyDsl() 
        {
            var jobPoller = TransformMany(
                (Tick tick) => new[] { new Trackable("wibble123" )});

            var trackingPoller = TransformMany(
                (Trackable job) => PollCarrier2(job.CarrierRef)
                                    .Select(raw => (job, raw)));
            
            var trackingMapper = Transform(
                ((Trackable, RawTracking) tup) => {
                    var (trackable, raw) = tup;
                    return MapRaw(trackable, raw);
                });
            
            var sink = new BufferBlock<Tracked>();

            LinkUp(jobPoller, trackingPoller);
            LinkUp(trackingPoller, trackingMapper);
            LinkUp(trackingMapper, sink);
            
            jobPoller.Post(new Tick(DateTime.MinValue));
            jobPoller.Complete();
            await trackingMapper.Completion;
            
            var trackeds = sink.GatherAll();
            trackeds.Length.ShouldBe(2);
            trackeds.ShouldContain(t => t.Info == "Hello!");
            trackeds.ShouldContain(t => t.Info == "Boo!");
        }
        
        
        static Task<RawTracking[]> PollCarrier(string carrierRef)
            => Task.FromResult(new[] {
                new RawTracking(carrierRef, "Hello!"), 
                new RawTracking(carrierRef, "Boo!")
            });
        
        static IObservable<RawTracking> PollCarrier2(string carrierRef)
            => new[] {
                new RawTracking(carrierRef, "Hello!"), 
                new RawTracking(carrierRef, "Boo!")
            }.ToObservable();

        static Task<Tracked> MapRaw(Trackable trackable, RawTracking raw)
            => Task.FromResult(new Tracked(trackable, raw.Info));

        
        
        class RawTracking {
            public readonly string Ref;
            public readonly string Info;
            
            public RawTracking(string @ref, string info) {
                Ref = @ref;
                Info = info;
            }
        }
        
        class Tick 
        {
            public readonly DateTime DateTime;
            
            public Tick(DateTime dateTime) {
                DateTime = dateTime;
            }
        }
        
        class Trackable 
        {
            public readonly string CarrierRef;
            
            public Trackable(string carrierRef) {
                CarrierRef = carrierRef;
            }
        }

        class Tracked 
        {
            public readonly Trackable Trackable;
            public readonly string Info;
            
            public Tracked(Trackable trackable, string info) {
                Trackable = trackable;
                Info = info;
            }
        }

        
    }

    public static class Extensions
    {
        public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
            => Task.WhenAll(tasks);

        public static T[] GatherAll<T>(this BufferBlock<T> block)
            => block.TryReceiveAll(out var items) 
                ? items.ToArray() 
                : new T[0];

        public static Task<IEnumerable<T>> ToTaskEnumerable<T>(this IObservable<T> source)
            => source.ToArray().Cast<IEnumerable<T>>().LastAsync().ToTask();
    }
    
    public static class DataFlowDsl 
    {
        public static TransformManyBlock<A, B> TransformMany<A, B>(Func<A, IEnumerable<B>> fn)
            => new TransformManyBlock<A, B>(fn);
        
        public static TransformManyBlock<A, B> TransformMany<A, B>(Func<A, Task<IEnumerable<B>>> fn)
            => new TransformManyBlock<A, B>(fn);

        public static TransformManyBlock<A, B> TransformMany<A, B>(Func<A, IObservable<B>> fn)
            => TransformMany<A, B>(a => fn(a).ToTaskEnumerable());
        
        public static TransformBlock<A, B> Transform<A, B>(Func<A, B> fn)
            => new TransformBlock<A, B>(fn);
        
        public static TransformBlock<A, B> Transform<A, B>(Func<A, Task<B>> fn)
            => new TransformBlock<A, B>(fn);
        
        public static void LinkUp<T>(ISourceBlock<T> source, ITargetBlock<T> target)
            => source.LinkTo(target, new DataflowLinkOptions { PropagateCompletion = true });
    }
    
}