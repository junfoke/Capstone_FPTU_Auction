using BE_AuctionOT_Cronjob.Modelss;
using BE_AuctionOT_Cronjob.RabbitMQ.BidQueue.Publishers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace BE_AuctionOT_Cronjob.Job
{
    public class AutoBidJob : IJob
    {
        private readonly DB_AuctionAOTContext _context;
        private readonly IBidPublisher _bidPublisher;
        private readonly ISchedulerFactory _schedulerFactory;

        public AutoBidJob(DB_AuctionAOTContext context, IBidPublisher bidPublisher, ISchedulerFactory schedulerFactory)
        {
            _context = context;
            _bidPublisher = bidPublisher;
            _schedulerFactory = schedulerFactory;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                Console.WriteLine("AutoBidJob Start");

                var autoBids = _context.AutoBids.ToList();
                foreach (var autoBid in autoBids)
                {
                    var scheduler = await _schedulerFactory.GetScheduler();
                    var jobKey = new JobKey($"ProcessBidJob-{autoBid.AutoBidId}", "BiddingGroup");

                    // Check if the job already exists
                    if (await scheduler.CheckExists(jobKey))
                    {
                        Console.WriteLine($"Job for AutoBidId {autoBid.AutoBidId} already exists. Skipping.");
                        continue;
                    }

                    var job = JobBuilder.Create<ProcessBidJob>()
                        .WithIdentity(jobKey)
                        .UsingJobData("AutoBidId", autoBid.AutoBidId)
                        .Build();

                    var trigger = TriggerBuilder.Create()
                        .WithIdentity($"Trigger-{autoBid.AutoBidId}", "BiddingGroup")
                        .StartNow()
                        .WithSimpleSchedule(x => x.WithIntervalInSeconds(30).RepeatForever()) //Run every 30 seconds :
                        .Build();

                    await scheduler.ScheduleJob(job, trigger);
                }

                Console.WriteLine("AutoBidJob End");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error AutoBidJob: {ex}");
            }
        }
    }
}
