using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Akka.Actor;
using Akka.Persistence;
using Akka.Util.Internal;

namespace PersistentActorExample
{
    class Crash
    {
    }

    class Start
    {
    }

    class GetAllMessages
    {
    }

    class AllMessages
    {
        public IReadOnlyList<string> History { get; }

        public AllMessages(List<string> history)
        {
            History = history.AsReadOnly();
        }
    }

    public class MessageHistory : ReceivePersistentActor
    {
        private List<string> _history = new List<string>();
        public override string PersistenceId => Self.Path.ToString();

        public MessageHistory()
        {
            Command<Crash>(x => throw new ArgumentException("Crash error"));
            Command<GetAllMessages>(x => Sender.Tell(new AllMessages(_history)));

            Recover<string>(x => _history.Add(x));
            Command<string>(x => Persist(x, s =>
            {
                _history.Add(s);
            }));
        }
    }

    public class ParentActor : ReceiveActor
    {
        public ParentActor()
        {
            Receive<Start>(x =>
            {
                var props = Props.Create(() => new MessageHistory());
                var actor = Context.ActorOf(props);

                actor.Tell("Hej, što ima?");
                actor.Tell("heyyyyy");
                actor.Tell(new Crash());
                actor.Tell("Oporavio sam se");

                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(1), actor, new GetAllMessages(), Self);
                //actor.Tell(new GetAllMessages());
            });
            Receive<AllMessages>(x => x.History.ForEach(Console.WriteLine));
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("persistence-example"))
            {
                var props = Props.Create(() => new ParentActor());
                var actor = system.ActorOf(props);
                actor.Tell(new Start());

                Console.ReadLine();
            }
        }
    }
}