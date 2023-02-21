using AutoMapper;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;
        private IMapper _mapper;


        public CartRepository(
            ApplicationDbContext context, 
            IMapper mapper
        )
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> ClearCart(string userId)
        {
            var cartHeaderFromdb = await _context.CartHeaders.FirstOrDefaultAsync( x => x.UserId == userId );
            if ( cartHeaderFromdb != null )
            {
                _context.CartDetails.RemoveRange(_context.CartDetails.Where(u => u.CartHeaderId == cartHeaderFromdb.CardHeaderId));
                _context.CartHeaders.Remove(cartHeaderFromdb);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<CartDto> CreateUpdateCart(CartDto cartDto)
        {
            var cart = _mapper.Map<Cart>(cartDto);

            var prodInDb = _context.Products
                .FirstOrDefault(u => u.ProductId == cartDto.CartDetails.FirstOrDefault().ProductId);

            if(prodInDb == null)
            {
                _context.Products.Add(cart.CartDetails.FirstOrDefault().Product);
                await _context.SaveChangesAsync();
            }

            var cartHeaderFromDb = await _context.CartHeaders.AsNoTracking().FirstOrDefaultAsync(u => 
                u.UserId == cart.CartHeader.UserId
            );

            if(cartHeaderFromDb == null )
            {
                _context.CartHeaders.Add(cart.CartHeader);
                await _context.SaveChangesAsync();
                cart.CartDetails.FirstOrDefault().CartHeaderId = cart.CartHeader.CardHeaderId;
                cart.CartDetails.FirstOrDefault().Product = null;
                _context.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                await _context.SaveChangesAsync();
            }
            else
            {
                var cartDetailsFromDb = await _context.CartDetails.AsNoTracking().FirstOrDefaultAsync(u =>
                    u.ProductId == cart.CartDetails.FirstOrDefault().ProductId &&
                    u.CartHeaderId == cartHeaderFromDb.CardHeaderId
                );

                if(cartDetailsFromDb == null)
                {
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartHeaderFromDb.CardHeaderId;
                    cart.CartDetails.FirstOrDefault().Product = null;
                    _context.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                    await _context.SaveChangesAsync();
                } 
                else
                {
                    cart.CartDetails.FirstOrDefault().Count += cartDetailsFromDb.Count;
                    cart.CartDetails.FirstOrDefault().Product = null;
                    cart.CartDetails.FirstOrDefault().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartDetailsFromDb.CartHeaderId;
                    _context.CartDetails.Update(cart.CartDetails.FirstOrDefault());
                    await _context.SaveChangesAsync();
                }
            }

            return _mapper.Map<CartDto>(cart);
        }

        public async Task<CartDto> GetCartByUserId(string userId)
        {
            Cart cart = new()
            {
                CartHeader = await _context.CartHeaders.FirstOrDefaultAsync(x => x.UserId == userId)
            };
            cart.CartDetails = _context.CartDetails
                .Where(u => u.CartHeaderId == cart.CartHeader.CardHeaderId)
                .Include(u => u.Product);

            return _mapper.Map<CartDto>(cart);
        }   

        public async Task<bool> RemoveFromCart(int cartDetailsId)
        {
            try
            {
                var cartDetails = await _context.CartDetails.FirstOrDefaultAsync(u => u.CartDetailsId == cartDetailsId);
                var totalCountOfCartItems = _context.CartDetails.Where(u => u.CartHeaderId == cartDetails.CartHeaderId).Count();

                _context.CartDetails.Remove(cartDetails);
                if (totalCountOfCartItems == 1)
                {
                    var cartHeaderToRemove = await _context.CartHeaders
                        .FirstOrDefaultAsync(u => u.CardHeaderId == cartDetails.CartHeaderId);

                    _context.CartHeaders.Remove(cartHeaderToRemove);
                }
                await _context.SaveChangesAsync();
                return true;
            } 
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> ApplyCoupon(string userId, string couponCode)
        {
            var cartFromDb = await _context.CartHeaders.FirstOrDefaultAsync(c => c.UserId == userId);
            cartFromDb.CouponCode = couponCode;
            _context.CartHeaders.Update(cartFromDb);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveCoupon(string userId)
        {
            var cartFromDb = await _context.CartHeaders.FirstOrDefaultAsync(c => c.UserId == userId);
            cartFromDb.CouponCode = string.Empty;
            _context.CartHeaders.Update(cartFromDb);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
