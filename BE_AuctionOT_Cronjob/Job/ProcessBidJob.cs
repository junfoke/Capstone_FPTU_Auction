using BE_AuctionOT_Cronjob.Modelss;
using BE_AuctionOT_Cronjob.RabbitMQ.BidQueue.Publishers;
using Microsoft.EntityFrameworkCore;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BE_AuctionOT_Cronjob.Job
{
    public class ProcessBidJob : IJob
    {
        private readonly DB_AuctionAOTContext _context;
        private readonly IBidPublisher _bidPublisher;

        public ProcessBidJob(DB_AuctionAOTContext context, IBidPublisher bidPublisher)
        {
            _context = context;
            _bidPublisher = bidPublisher;
        }

        public async Task Execute(IJobExecutionContext jobContext)
        {
            var autoBidId = jobContext.JobDetail.JobDataMap.GetInt("AutoBidId");
            try
            {
                var autoBid = _context.AutoBids.FirstOrDefault(ab => ab.AutoBidId == autoBidId);
                if (autoBid == null) return;

                var auction = _context.Auctions.FirstOrDefault(a => a.AuctionId == autoBid.AuctionId);
                if (auction == null || DateTime.Now >= auction.EndTime) return;

                //var lastAuctionBidByUserId = _context.AuctionBids
                //                            .Where(a => a.AuctionId == autoBid.AuctionId && a.UserId == autoBid.UserId)
                //                            .OrderByDescending(a => a.BidTime)
                //                            .FirstOrDefault();
                // Check invalid: < 15 seconds since last bid => terminate
                //if (lastAuctionBidByUserId != null && (DateTime.Now - lastAuctionBidByUserId.BidTime.Value).TotalSeconds < 15)
                //{
                //    return;
                //}

                //Check first bid
                var listAuctionBidByAuctionIdAndUserId = _context.AuctionBids.Where(b => b.AuctionId == autoBid.AuctionId && b.UserId == autoBid.UserId).ToList();
                if (listAuctionBidByAuctionIdAndUserId.Count == 0)
                {
                    //Minus deposite amount
                    var userCurrentPoint = _context.Points.FirstOrDefault(o => o.UserId == autoBid.UserId);
                    if (userCurrentPoint == null || userCurrentPoint.PointsAmount < ((int)auction.DepositAmount))
                    {
                        return;
                    }
                    else
                    {
                        var point = _context.Points.FirstOrDefault(o => o.UserId == autoBid.UserId);
                        if (point == null)
                        {
                            return;
                        }
                        point.PointsAmount = (userCurrentPoint.PointsAmount - (int)auction.DepositAmount);
                        _context.Points.Update(point);
                        _context.SaveChanges();
                    }
                }

                var auctionBids = _context.AuctionBids
                                            .Where(a => a.AuctionId == autoBid.AuctionId)
                                            .ToList();
                //if (auctionBids.Any())
                //{
                // Find the maximum bid and corresponding auction bid
                var maxAuctionBid = auctionBids.OrderByDescending(b => b.BidAmount).FirstOrDefault();
                if (maxAuctionBid != null && maxAuctionBid.UserId != autoBid.UserId && (((int)maxAuctionBid.BidAmount + (int)auction.StepPrice)) <= (int)autoBid.MaxBidAmount)
                {
                    //Bid for him
                    Bid bid = new Bid()
                    {
                        AuctionId = (int)auction.AuctionId,
                        UserId = Int32.Parse(autoBid.UserId + ""),
                        BidAmount = maxAuctionBid.BidAmount + auction.StepPrice,
                        Timestamp = DateTime.Now,
                    };
                    _bidPublisher.PublishBid(bid);
                }

                if (maxAuctionBid == null)
                {
                    Bid bid = new Bid()
                    {
                        AuctionId = (int)auction.AuctionId,
                        UserId = Int32.Parse(autoBid.UserId + ""),
                        BidAmount = auction.StepPrice,
                        Timestamp = DateTime.Now,
                    };
                    _bidPublisher.PublishBid(bid);
                }
                Console.WriteLine($"Processing bid for AutoBidId: {autoBidId}");

                // Your bid logic goes here...
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessBidJob for AutoBidId {autoBidId}: {ex}");
            }
        }
    }

}
