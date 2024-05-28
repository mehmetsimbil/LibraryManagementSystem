using Application.Features.Members.Rules;
using Application.Services.Repositories;
using AutoMapper;
using Domain.Entities;
using NArchitecture.Core.Application.Pipelines.Caching;
using NArchitecture.Core.Application.Pipelines.Logging;
using NArchitecture.Core.Application.Pipelines.Transaction;
using MediatR;
using Application.Services.UsersService;
using Application.Services.AuthService;
using NArchitecture.Core.Application.Dtos;
using MimeKit;
using NArchitecture.Core.Mailing;

namespace Application.Features.Members.Commands.Create;

public class CreateMemberCommand : IRequest<CreatedMemberResponse>, ICacheRemoverRequest, ILoggableRequest, ITransactionalRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
   

    public bool BypassCache { get; }
    public string? CacheKey { get; }
    public string[]? CacheGroupKey => ["GetMembers"];

    public class CreateMemberCommandHandler : IRequestHandler<CreateMemberCommand, CreatedMemberResponse>
    {
        private readonly IMapper _mapper;
        private readonly IMemberRepository _memberRepository;
        private readonly MemberBusinessRules _memberBusinessRules;
        private readonly IAuthService _authService;
        private readonly IMailService _mailService;

        public CreateMemberCommandHandler(IMapper mapper, IMemberRepository memberRepository,
                                         MemberBusinessRules memberBusinessRules, IAuthService authService,IMailService mailService)
        {
            _mapper = mapper;
            _memberRepository = memberRepository;
            _memberBusinessRules = memberBusinessRules;
            _authService = authService;
            _mailService = mailService;
        }

        public async Task<CreatedMemberResponse> Handle(CreateMemberCommand request, CancellationToken cancellationToken)
        {
            User User = await _authService.Register(new UserForRegisterDto()
            {
                Email = request.Email,
                Password = request.Password,
            });

            Member member = _mapper.Map<Member>(request);
            member.UserId = User.Id;

           
            await _memberRepository.AddAsync(member);
            Mail mail = new Mail(
                subject: "Kayit Basarili",
                textBody: " Kayit Basarili, Hosgeldiniz " + request.FirstName,
                htmlBody: "<p>Kayit Basarili, Hosgeldiniz</p>" + request.FirstName,
                new List<MailboxAddress>()
                { new(request.FirstName+" "+request.LastName,request.Email)}
                );
            await _mailService.SendEmailAsync(mail);

            CreatedMemberResponse response = _mapper.Map<CreatedMemberResponse>(member);
            return response;
        }
    }
}