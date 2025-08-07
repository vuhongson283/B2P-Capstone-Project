using B2P_API.DTOs.RatingDTO;
using B2P_API.Interface;
using B2P_API.Models;
using B2P_API.Response;

namespace B2P_API.Services
{
    public class RatingService
    {
        private readonly IRatingRepository _repo;

        public RatingService(IRatingRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiResponse<IEnumerable<ResponseRatingDto>>> GetAllAsync()
        {
            var ratings = await _repo.GetAllAsync();
            var dtoList = ratings.Select(r => new ResponseRatingDto
            {
                RatingId = r.RatingId,
                BookingId = r.BookingId ?? 0,
                Comment = r.Comment,
                CreateAt = r.CreateAt.Value,
                Stars = r.Stars ?? 0
            });

            return new ApiResponse<IEnumerable<ResponseRatingDto>>
            {
                Success = true,
                Message = "Lấy danh sách đánh giá thành công.",
                Status = 200,
                Data = dtoList
            };
        }

        public async Task<ApiResponse<ResponseRatingDto>> GetByIdAsync(int id)
        {
            var rating = await _repo.GetByIdAsync(id);
            if (rating == null)
            {
                return new ApiResponse<ResponseRatingDto>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy đánh giá.",
                    Data = null
                };
            }

            return new ApiResponse<ResponseRatingDto>
            {
                Success = true,
                Status = 200,
                Message = "Thành công.",
                Data = new ResponseRatingDto
                {
                    RatingId = rating.RatingId,
                    BookingId = rating.BookingId ?? 0,
                    Comment = rating.Comment,
                    CreateAt = rating.CreateAt.Value,
                    Stars = rating.Stars ?? 0
                }
            };
        }

        public async Task<ApiResponse<ResponseRatingDto>> CreateAsync(CreateRatingDto dto)
        {
            var rating = new Rating
            {
                BookingId = dto.BookingId,
                Comment = dto.Comment,
                Stars = dto.Stars,
                CreateAt = DateTime.Now
            };

            await _repo.AddAsync(rating);

            return new ApiResponse<ResponseRatingDto>
            {
                Success = true,
                Status = 201,
                Message = "Tạo đánh giá thành công.",
                Data = new ResponseRatingDto
                {
                    RatingId = rating.RatingId,
                    BookingId = rating.BookingId ?? 0,
                    Comment = rating.Comment,
                    CreateAt = rating.CreateAt.Value,
                    Stars = rating.Stars ?? 0
                }
            };
        }

        public async Task<ApiResponse<string>> UpdateAsync(int id, CreateRatingDto dto)
        {
            var rating = await _repo.GetByIdAsync(id);
            if (rating == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy đánh giá.",
                    Data = null
                };
            }

            rating.BookingId = dto.BookingId;
            rating.Comment = dto.Comment;
            rating.Stars = dto.Stars;

            await _repo.UpdateAsync(rating);

            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Cập nhật đánh giá thành công.",
                Data = "OK"
            };
        }

        public async Task<ApiResponse<string>> DeleteAsync(int id)
        {
            var rating = await _repo.GetByIdAsync(id);
            if (rating == null)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Status = 404,
                    Message = "Không tìm thấy đánh giá.",
                    Data = null
                };
            }

            await _repo.DeleteAsync(rating);

            return new ApiResponse<string>
            {
                Success = true,
                Status = 200,
                Message = "Xóa đánh giá thành công.",
                Data = "OK"
            };
        }
    }

}
