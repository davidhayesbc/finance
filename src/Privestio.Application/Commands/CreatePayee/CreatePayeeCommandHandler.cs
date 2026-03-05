using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.CreatePayee;

public class CreatePayeeCommandHandler : IRequestHandler<CreatePayeeCommand, PayeeResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePayeeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PayeeResponse> Handle(
        CreatePayeeCommand request,
        CancellationToken cancellationToken
    )
    {
        var payee = new Payee(request.DisplayName, request.OwnerId)
        {
            DefaultCategoryId = request.DefaultCategoryId,
        };

        await _unitOfWork.Payees.AddAsync(payee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PayeeResponse
        {
            Id = payee.Id,
            DisplayName = payee.DisplayName,
            DefaultCategoryId = payee.DefaultCategoryId,
            Aliases = payee.Aliases.ToList(),
        };
    }
}
