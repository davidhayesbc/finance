using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;
using Privestio.Domain.Entities;

namespace Privestio.Application.Commands.CreateExchangeRate;

public class CreateExchangeRateCommandHandler
    : IRequestHandler<CreateExchangeRateCommand, ExchangeRateResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateExchangeRateCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ExchangeRateResponse> Handle(
        CreateExchangeRateCommand request,
        CancellationToken cancellationToken
    )
    {
        var rate = new ExchangeRate(
            request.FromCurrency,
            request.ToCurrency,
            request.Rate,
            request.AsOfDate,
            request.Source
        );

        await _unitOfWork.ExchangeRates.AddAsync(rate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ExchangeRateMapper.ToResponse(rate);
    }
}
