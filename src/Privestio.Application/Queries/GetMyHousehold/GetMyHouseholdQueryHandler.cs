using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Application.Mapping;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetMyHousehold;

public class GetMyHouseholdQueryHandler : IRequestHandler<GetMyHouseholdQuery, HouseholdResponse?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMyHouseholdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<HouseholdResponse?> Handle(
        GetMyHouseholdQuery request,
        CancellationToken cancellationToken
    )
    {
        var household = await _unitOfWork.Households.GetByUserIdAsync(
            request.UserId,
            cancellationToken
        );

        return household is null ? null : HouseholdMapper.ToResponse(household);
    }
}
