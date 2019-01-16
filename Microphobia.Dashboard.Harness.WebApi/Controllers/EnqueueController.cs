using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using N17Solutions.Microphobia;

namespace Microphobia.Dashboard.Harness.WebApi.Controllers
{
    public abstract class EnqueueMe
    {
        public static void Method()
        {
            Console.WriteLine("Enqueue Me!");
        }    
    }

    public abstract class EnqueueFailure
    {
        public static void AlwaysFail()
        {
            throw new InvalidOperationException("I will always fail because I've been setup to do so.");
        }
    }

    public class InjectMe
    {
        public string Property => "Injected";
    }

    public class ComplicatedEnqueueMe
    {
        private readonly InjectMe _injectMe;

        public ComplicatedEnqueueMe(InjectMe injectMe)
        {
            _injectMe = injectMe;
        }

        public void Method()
        {
            Console.WriteLine($"Complicated Enqueue Me! - {_injectMe.Property}");
        }
    }

    [Route("api/[controller]")]
    public class EnqueueController : ControllerBase
    {
        private readonly Queue _queue;

        public EnqueueController(Queue queue)
        {
            _queue = queue;
        }

        [HttpPost]
        public async Task<IActionResult> Post(bool complicated, bool failure, bool multiple)
        {
            if (complicated)
                await _queue.Enqueue<ComplicatedEnqueueMe>(x => x.Method());
            else if (failure)
                await _queue.Enqueue(() => EnqueueFailure.AlwaysFail());
            else if (multiple)
            {
                await _queue.Enqueue<ComplicatedEnqueueMe>(x => x.Method());
                await _queue.Enqueue<EnqueueMe>(_ => EnqueueMe.Method());
            }
            else
                await _queue.Enqueue<EnqueueMe>(_ => EnqueueMe.Method());
            
            return Ok();
        }
    }
}