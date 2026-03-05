using MediatR;
using Privestio.Application.Interfaces;
using Privestio.Contracts.Responses;

namespace Privestio.Application.Queries.GetPayees;

public class GetPayeesQueryHandler : IRequestHandler<GetPayeesQuery, IReadOnlyList<PayeeResponse>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPayeesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<PayeeResponse>> Handle(
        GetPayeesQuery request,
        CancellationToken cancellationToken
    )
    {
        var payees = await _unitOfWork.Payees.GetByOwnerIdAsync(request.OwnerId, cancellationToken);

        return payees
            .Select(p => new PayeeResponse
            {
                Id = p.Id,
                DisplayName = p.DisplayName,
                DefaultCategoryId = p.DefaultCategoryId,
                Aliases = p.Aliases.ToList(),
            })
            .ToList();
    }
}
