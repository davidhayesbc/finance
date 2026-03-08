using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Commands.CreateSinkingFund;

public class CreateSinkingFundCommandHandler
    : IRequestHandler<CreateSinkingFundCommand, SinkingFundResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSinkingFundCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SinkingFundResponse> Handle(
        CreateSinkingFundCommand request,
        CancellationToken cancellationToken
    )
    {
        var fund = new SinkingFund(
            request.UserId,
            request.Name,
            new Money(request.TargetAmount, request.Currency),
            request.DueDate,
            request.AccountId,
            request.CategoryId,
            request.Notes
        );

        await _unitOfWork.SinkingFunds.AddAsync(fund, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return SinkingFundMapper.ToResponse(fund);
    }
}
