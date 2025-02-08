using BE_AuctionAOT.Common.Base.Entity;
using BE_AuctionAOT.Common.Constants;
using BE_AuctionAOT.Controllers.Posts;
using BE_AuctionAOT.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace BE_AuctionAOT.DAO.PostManagement.Post
{
    public class PostDao
    {
        private readonly DB_AuctionAOTContext _context;
        public PostDao(DB_AuctionAOTContext context)
        {
            _context = context;
        }

        public async Task<CreatePostOutputDto> CreatePost(Models.Post post)
        {
            try
            {
                var output = this.Output(ResultCd.SUCCESS).Create<CreatePostOutputDto>();
                await _context.AddAsync(post);
                await _context.SaveChangesAsync();
                var newId = post.Id;
                output.Id = newId;
                return output;
            }
            catch (Exception ex)
            {
                return this.Output(ResultCd.FAILURE).WithException(ex).Create<CreatePostOutputDto>();
            }
        }

        public async Task<List<object>> GetPosts(bool isAdmin)
        {
            try
            {
                var postsQuery = from post in _context.Posts
                                 join user in _context.UserProfiles on post.UserId equals user.UserId
                                 where isAdmin || post.Status == PostStatus.Approved
                                 select new
                                 {
                                     post_id = post.Id,
                                     title = post.Title,
                                     content = post.Content,
                                     status = post.Status,
                                     category = new Dropdown
                                     {
                                         Id = (long)post.CategoryId,
                                         Name = _context.Categories
                                             .Where(x => x.CategoryId == post.CategoryId)
                                             .Select(x => x.CategoryName)
                                             .First()
                                     },
                                     Images = _context.PostImages
                                         .Where(x => x.PostId == post.Id)
                                         .OrderBy(x => x.SortOrder)
                                         .Select(x => new
                                         {
                                             MediaUrl = x.MediaUrl,
                                             MediaType = x.MediaType,
                                         }).ToList(),
                                     create_at = post.CreatedAt,
                                     total_comment = _context.Comments.Count(c => c.PostId == post.Id),
                                     total_like = _context.Likes.Count(l => l.PostId == post.Id && l.DeletedAt == null),
                                     owner = new
                                     {
                                         name = user.FullName,
                                         avatar = user.Avatar
                                     },
                                 };

                var posts = await postsQuery
                    .OrderByDescending(x => x.create_at)
                    .ToListAsync();

                return posts.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<List<object>> GetPostByUserId(int UserId)
        {
            try
            {
                var postsQuery = from post in _context.Posts
                                 join user in _context.UserProfiles on post.UserId equals user.UserId
                                 where post.Status == PostStatus.Approved && post.UserId == UserId
                                 select new
                                 {
                                     post_id = post.Id,
                                     title = post.Title,
                                     content = post.Content,
                                     status = post.Status,
                                     category = new Dropdown { Id = (long)post.CategoryId, Name = _context.Categories.Where(x => x.CategoryId == post.CategoryId).Select(x => x.CategoryName).First() },
                                     Images = _context.PostImages.Where(x => x.PostId == post.Id)
                                     .OrderBy(x => x.SortOrder)
                                     .Select(x => new
                                     {
                                         MediaUrl = x.MediaUrl,
                                         MediaType = x.MediaType,
                                     }).ToList(),
                                     create_at = post.CreatedAt,
                                     total_comment = _context.Comments.Count(c => c.PostId == post.Id),
                                     total_like = _context.Likes.Count(l => l.PostId == post.Id && l.DeletedAt == null),
                                     owner = new
                                     {
                                         name = user.FullName,
                                         avatar = user.Avatar
                                     },
                                 };

                var posts = await postsQuery.OrderByDescending(x => x.create_at).ToListAsync();

                return posts.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<object>();
            }
        }

        public async Task<List<object>> getPostByUserIdAndAuctionId(int UserId, int AuctionId)
        {
            try
            {
                var postsQuery = from post in _context.Posts
                                 join user in _context.UserProfiles on post.UserId equals user.UserId
                                 join auctionPost in _context.AuctionPosts on post.Id equals auctionPost.PostId
                                 where post.Status == PostStatus.Approved && post.UserId == UserId && auctionPost.AuctionId == AuctionId
                                 select new
                                 {
                                     post_id = post.Id,
                                     title = post.Title,
                                     content = post.Content,
                                     status = post.Status,
                                     category = new Dropdown { Id = (long)post.CategoryId, Name = _context.Categories.Where(x => x.CategoryId == post.CategoryId).Select(x => x.CategoryName).First() },
                                     Images = _context.PostImages.Where(x => x.PostId == post.Id)
                                     .OrderBy(x => x.SortOrder)
                                     .Select(x => new
                                     {
                                         MediaUrl = x.MediaUrl,
                                         MediaType = x.MediaType,
                                     }).ToList(),
                                     create_at = post.CreatedAt,
                                     total_comment = _context.Comments.Count(c => c.PostId == post.Id),
                                     total_like = _context.Likes.Count(l => l.PostId == post.Id && l.DeletedAt == null),
                                     owner = new
                                     {
                                         name = user.FullName,
                                         avatar = user.Avatar
                                     },
                                 };

                var posts = await postsQuery.OrderByDescending(x => x.create_at).ToListAsync();

                return posts.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return new List<object>();
            }
        }


        public BaseOutputDto SaveImgPost(List<PostImage> inputDto)
        {
            try
            {
                var output = this.Output(ResultCd.SUCCESS).Create<BaseOutputDto>();

                _context.AddRange(inputDto);
                _context.SaveChanges();
                return output;
            }
            catch (Exception ex)
            {
                return this.Output(ResultCd.FAILURE).WithException(ex).Create<BaseOutputDto>();
            }
        }
    }
}
