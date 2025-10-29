using AutoMapper;
using BookManagementSystemAPI.Data;
using BookManagementSystemAPI.Dto;
using BookManagementSystemAPI.Models;
using BookManagementSystemAPI.Repositories;

namespace BookManagementSystemAPI.Services
{
    public class BookService : IBookService
    {
        //从UNSW BookRepo 改成了USYD BookRepo
        //UNSWBookRepo 还是别的 都是这个接口的实现，传哪个都满足参数的要求
        private readonly IBookRepository _bookRepository;
        private readonly IMapper _mapper;
        private readonly MongoDbContext _mongoDbContext;

        private readonly ICacheService _cacheService;

        private readonly ILogger<BookService> _logger;
        //对象在注入时， 到底是什么机制或者东西把它实例化了？？？
        //IOC container , 依赖注入容器
        //依赖注入的容器 会自动创建注入的类型的实例， 前提是我们已经告诉他， 怎么创建和创建谁
        public BookService(IBookRepository bookRepository, IMapper mapper, MongoDbContext mongoDbContext,
                            ICacheService cacheService, ILogger<BookService> logger)
        {
            _bookRepository = bookRepository;
            _mapper = mapper;
            _mongoDbContext = mongoDbContext;
            _cacheService = cacheService;
            _logger = logger;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bookCreateRequest"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Book> CreateBook(BookCreateRequest bookCreateRequest)
        {
            Book newBook = _mapper.Map<Book>(bookCreateRequest);
            //如果直接存的这个book, 前端瞎传参数， 把Id传进来了， 会发生什么？
            if (newBook.Id > 0)
            {
                throw new ArgumentException($"Book Id is invalid, value : {newBook.Id}");
            }

            if (!string.IsNullOrWhiteSpace(newBook.Name) && !string.IsNullOrWhiteSpace(newBook.Description))
            {

                Book book = await _bookRepository.CreateBook(newBook);

                var expirationTime = DateTimeOffset.Now.AddMinutes(5);

                var success = _cacheService.SetData<Book>($"book_{book.Id}", book, expirationTime);

                _logger.LogInformation($"cached book result: {success}");
                


            //    //add book event here
            //    BookEvent bookEvent = new BookEvent() { BookId = book.Id, EventNote = $"Book created :{book.Id}"};
            //    await _mongoDbContext.BookEvents.InsertOneAsync(bookEvent);
               return book;
            }

            throw new ArgumentException("Book name or description cannot be null or empty");
        }

        /// <summary>
        /// Get book by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Book GetBookById(int id)
        {
            var cachedBook = _cacheService.GetData<Book>($"book_{id}");
            if (cachedBook != null)
            {
                return cachedBook;
            }
            
            Book book = _bookRepository.GetBookById(id);
            return book;
        }

        public Book GetBookByName(string name)
        {
            throw new NotImplementedException();
        }

        public List<Book> GetBooks()
        {
            List<Book> books = _bookRepository.GetAllBooks();
            return books;
        }
    }
}
