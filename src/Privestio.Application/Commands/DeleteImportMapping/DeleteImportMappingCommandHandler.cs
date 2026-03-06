using MediatR;
using Privestio.Application.Interfaces;

namespace Privestio.Application.Commands.DeleteImportMapping;

public class DeleteImportMappingCommandHandler : IRequestHandler<DeleteImportMappingCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteImportMappingCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(
        DeleteImportMappingCommand request,
        CancellationToken cancellationToken
    )
    {
        var mapping = await _unitOfWork.ImportMappings.GetByIdAsync(request.Id, cancellationToken);
        if (mapping is null || mapping.UserId != request.UserId)
            return false;

        await _unitOfWork.ImportMappings.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
