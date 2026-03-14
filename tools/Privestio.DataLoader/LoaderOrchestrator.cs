using Microsoft.Extensions.Logging;
using Privestio.Contracts.Requests;
using Privestio.Contracts.Responses;

namespace Privestio.DataLoader;

public class LoaderOrchestrator
{
    private readonly ApiClient _api;
    private readonly DataLoaderConfig _config;
    private readonly string _dataDir;
    private readonly bool _dryRun;
    private readonly bool _verboseImportErrors;
    private readonly ILogger<LoaderOrchestrator> _logger;

    private readonly Dictionary<string, Guid> _accountNameToId = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, Guid> _categoryNameToId = new(
        StringComparer.OrdinalIgnoreCase
    );
    private readonly Dictionary<string, Guid> _mappingNameToId = new(
        StringComparer.OrdinalIgnoreCase
    );

    private int _created;
    private int _skipped;
    private int _failed;

    public LoaderOrchestrator(
        ApiClient api,
        DataLoaderConfig config,
        string dataDir,
        bool dryRun,
        bool verboseImportErrors,
        ILogger<LoaderOrchestrator> logger
    )
    {
        _api = api;
        _config = config;
        _dataDir = dataDir;
        _dryRun = dryRun;
        _verboseImportErrors = verboseImportErrors;
        _logger = logger;
    }

    public async Task<int> RunAsync()
    {
        _logger.LogInformation("Starting data load{DryRun}...", _dryRun ? " (DRY RUN)" : "");

        if (!await AuthenticateAsync())
            return 1;

        await LoadCategoriesAsync();
        await LoadPayeesAsync();
        await LoadTagsAsync();
        await LoadImportMappingsAsync();
        await LoadAccountsAsync();
        await LoadImportsAsync();
        await LoadValuationsAsync();
        await LoadPriceHistoryAsync();
        await ApplyAdjustmentsAsync();

        _logger.LogInformation(
            "Data load complete. Created: {Created}, Skipped: {Skipped}, Failed: {Failed}",
            _created,
            _skipped,
            _failed
        );

        return _failed > 0 ? 1 : 0;
    }

    private async Task<bool> AuthenticateAsync()
    {
        var auth = _config.Auth;
        _logger.LogInformation("Authenticating as {Email}...", auth.Email);

        if (_dryRun)
        {
            _logger.LogInformation("[DRY RUN] Would authenticate as {Email}", auth.Email);
            return true;
        }

        var response = await _api.LoginAsync(auth.Email, auth.Password);
        if (response is null && auth.Register)
        {
            _logger.LogInformation("Login failed, registering new user...");
            response = await _api.RegisterAsync(auth.Email, auth.Password, auth.DisplayName);
        }

        if (response is null)
        {
            _logger.LogError("Authentication failed for {Email}", auth.Email);
            return false;
        }

        _api.SetToken(response.AccessToken);
        _logger.LogInformation(
            "Authenticated as {Email} (user: {UserId})",
            auth.Email,
            response.UserId
        );
        return true;
    }

    private async Task LoadCategoriesAsync()
    {
        if (_config.Categories.Count == 0)
            return;

        _logger.LogInformation("Loading {Count} categories...", _config.Categories.Count);

        var existing = _dryRun ? [] : await _api.GetCategoriesAsync();
        var existingNames = existing.ToLookup(c => c.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var cat in existing)
            _categoryNameToId.TryAdd(cat.Name, cat.Id);

        foreach (var cat in _config.Categories)
        {
            if (existingNames.Contains(cat.Name))
            {
                _logger.LogInformation("  Category '{Name}' already exists, skipping", cat.Name);
                _skipped++;
                continue;
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would create category '{Name}'", cat.Name);
                continue;
            }

            Guid? parentId = null;
            if (
                !string.IsNullOrEmpty(cat.ParentCategory)
                && _categoryNameToId.TryGetValue(cat.ParentCategory, out var pid)
            )
            {
                parentId = pid;
            }

            var result = await _api.CreateCategoryAsync(
                new CreateCategoryRequest(cat.Name, cat.Type, cat.Icon, cat.SortOrder, parentId)
            );
            if (result is not null)
            {
                _categoryNameToId[result.Name] = result.Id;
                _logger.LogInformation("  Created category '{Name}'", cat.Name);
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to create category '{Name}'", cat.Name);
                _failed++;
            }
        }
    }

    private async Task LoadPayeesAsync()
    {
        if (_config.Payees.Count == 0)
            return;

        _logger.LogInformation("Loading {Count} payees...", _config.Payees.Count);

        var existing = _dryRun ? [] : await _api.GetPayeesAsync();
        var existingNames = existing.ToLookup(p => p.DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var payee in _config.Payees)
        {
            if (existingNames.Contains(payee.DisplayName))
            {
                _logger.LogInformation(
                    "  Payee '{Name}' already exists, skipping",
                    payee.DisplayName
                );
                _skipped++;
                continue;
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would create payee '{Name}'", payee.DisplayName);
                continue;
            }

            Guid? defaultCategoryId = null;
            if (
                !string.IsNullOrEmpty(payee.DefaultCategory)
                && _categoryNameToId.TryGetValue(payee.DefaultCategory, out var catId)
            )
            {
                defaultCategoryId = catId;
            }

            var result = await _api.CreatePayeeAsync(
                new CreatePayeeRequest(payee.DisplayName, defaultCategoryId)
            );
            if (result is not null)
            {
                _logger.LogInformation("  Created payee '{Name}'", payee.DisplayName);
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to create payee '{Name}'", payee.DisplayName);
                _failed++;
            }
        }
    }

    private async Task LoadTagsAsync()
    {
        if (_config.Tags.Count == 0)
            return;

        _logger.LogInformation("Loading {Count} tags...", _config.Tags.Count);

        var existing = _dryRun ? [] : await _api.GetTagsAsync();
        var existingNames = existing.ToLookup(t => t.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in _config.Tags)
        {
            if (existingNames.Contains(tag.Name))
            {
                _logger.LogInformation("  Tag '{Name}' already exists, skipping", tag.Name);
                _skipped++;
                continue;
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would create tag '{Name}'", tag.Name);
                continue;
            }

            var result = await _api.CreateTagAsync(new CreateTagRequest(tag.Name));
            if (result is not null)
            {
                _logger.LogInformation("  Created tag '{Name}'", tag.Name);
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to create tag '{Name}'", tag.Name);
                _failed++;
            }
        }
    }

    private async Task LoadImportMappingsAsync()
    {
        if (_config.ImportMappings.Count == 0)
            return;

        _logger.LogInformation("Loading {Count} import mappings...", _config.ImportMappings.Count);

        var existing = _dryRun ? [] : await _api.GetImportMappingsAsync();
        var existingNames = existing.ToLookup(m => m.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var m in existing)
            _mappingNameToId.TryAdd(m.Name, m.Id);

        foreach (var mapping in _config.ImportMappings)
        {
            if (existingNames.Contains(mapping.Name))
            {
                _logger.LogInformation("  Mapping '{Name}' already exists, skipping", mapping.Name);
                _skipped++;
                continue;
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would create mapping '{Name}'", mapping.Name);
                continue;
            }

            var result = await _api.CreateImportMappingAsync(
                new CreateImportMappingRequest
                {
                    Name = mapping.Name,
                    FileFormat = mapping.FileFormat,
                    Institution = mapping.Institution,
                    ColumnMappings = mapping.ColumnMappings,
                    DateFormat = mapping.DateFormat,
                    HasHeaderRow = mapping.HasHeaderRow,
                    AmountDebitColumn = mapping.AmountDebitColumn,
                    AmountCreditColumn = mapping.AmountCreditColumn,
                }
            );
            if (result is not null)
            {
                _mappingNameToId[result.Name] = result.Id;
                _logger.LogInformation("  Created mapping '{Name}'", mapping.Name);
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to create mapping '{Name}'", mapping.Name);
                _failed++;
            }
        }
    }

    private async Task LoadAccountsAsync()
    {
        if (_config.Accounts.Count == 0)
            return;

        _logger.LogInformation("Loading {Count} accounts...", _config.Accounts.Count);

        var existing = _dryRun ? [] : await _api.GetAccountsAsync();
        var existingNames = existing.ToLookup(a => a.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var a in existing)
            _accountNameToId.TryAdd(a.Name, a.Id);

        foreach (var account in _config.Accounts)
        {
            if (existingNames.Contains(account.Name))
            {
                _logger.LogInformation("  Account '{Name}' already exists, skipping", account.Name);
                _skipped++;
                continue;
            }

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would create account '{Name}'", account.Name);
                continue;
            }

            var result = await _api.CreateAccountAsync(
                new CreateAccountRequest
                {
                    Name = account.Name,
                    AccountType = account.AccountType,
                    AccountSubType = account.AccountSubType,
                    Currency = account.Currency,
                    Institution = account.Institution,
                    AccountNumber = account.AccountNumber,
                    OpeningBalance = account.OpeningBalance,
                    OpeningDate = DateOnly.Parse(account.OpeningDate),
                    Notes = account.Notes,
                }
            );
            if (result is not null)
            {
                _accountNameToId[result.Name] = result.Id;
                _logger.LogInformation(
                    "  Created account '{Name}' ({Id})",
                    account.Name,
                    result.Id
                );
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to create account '{Name}'", account.Name);
                _failed++;
            }
        }
    }

    private async Task LoadImportsAsync()
    {
        var accountsWithImports = _config.Accounts.Where(a => a.Imports.Count > 0).ToList();
        if (accountsWithImports.Count == 0)
            return;

        _logger.LogInformation("Processing file imports...");

        foreach (var account in accountsWithImports)
        {
            if (!_accountNameToId.TryGetValue(account.Name, out var accountId))
            {
                _logger.LogWarning("  Account '{Name}' not found, skipping imports", account.Name);
                _failed += account.Imports.Count;
                continue;
            }

            foreach (var import in account.Imports)
            {
                var filePath = Path.Combine(_dataDir, import.File);
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("  Import file not found: {File}", filePath);
                    _failed++;
                    continue;
                }

                if (_dryRun)
                {
                    _logger.LogInformation(
                        "[DRY RUN] Would import {File} into account '{Account}'",
                        import.File,
                        account.Name
                    );
                    continue;
                }

                Guid? mappingId = null;
                if (
                    !string.IsNullOrEmpty(import.MappingName)
                    && _mappingNameToId.TryGetValue(import.MappingName, out var mid)
                )
                {
                    mappingId = mid;
                }

                var policy = import.Policy.ToString();
                var result = await _api.ImportFileAsync(accountId, filePath, mappingId, policy);
                if (result is not null)
                {
                    _logger.LogInformation(
                        "  Imported {File} into '{Account}': {Imported} imported, {Duplicates} duplicates, {Errors} errors",
                        import.File,
                        account.Name,
                        result.ImportedCount,
                        result.DuplicateCount,
                        result.ErrorCount
                    );
                    _created += result.ImportedCount;
                    _skipped += result.DuplicateCount;
                    if (result.ErrorCount > 0)
                    {
                        if (_verboseImportErrors)
                        {
                            foreach (var err in result.Errors)
                            {
                                _logger.LogWarning(
                                    "    Import row error in {File} row {Row}: {Message}. Raw: {RawData}",
                                    import.File,
                                    err.RowNumber,
                                    err.Message,
                                    string.IsNullOrWhiteSpace(err.RawData) ? "<none>" : err.RawData
                                );
                            }

                            if (result.Errors.Count == 0)
                            {
                                _logger.LogWarning(
                                    "    Import reported {ErrorCount} errors but did not include row details.",
                                    result.ErrorCount
                                );
                            }
                        }
                        else
                        {
                            _logger.LogWarning(
                                "    Import had {ErrorCount} row error(s). Re-run with --verbose-import-errors to print row details.",
                                result.ErrorCount
                            );
                        }

                        _failed += result.ErrorCount;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "  Failed to import {File} into '{Account}'",
                        import.File,
                        account.Name
                    );
                    _failed++;
                }
            }
        }
    }

    private async Task LoadValuationsAsync()
    {
        var accountsWithValuations = _config.Accounts.Where(a => a.Valuations.Count > 0).ToList();
        if (accountsWithValuations.Count == 0)
            return;

        _logger.LogInformation("Loading valuations...");

        foreach (var account in accountsWithValuations)
        {
            if (!_accountNameToId.TryGetValue(account.Name, out var accountId))
            {
                _logger.LogWarning(
                    "  Account '{Name}' not found, skipping valuations",
                    account.Name
                );
                _failed += account.Valuations.Count;
                continue;
            }

            var existingValuations = _dryRun ? [] : await _api.GetValuationsAsync(accountId);
            var existingKeys = existingValuations
                .Select(v => (v.EffectiveDate, v.Source))
                .ToHashSet();

            foreach (var val in account.Valuations)
            {
                var effectiveDate = DateOnly.Parse(val.EffectiveDate);
                if (existingKeys.Contains((effectiveDate, val.Source)))
                {
                    _logger.LogInformation(
                        "  Valuation for '{Account}' on {Date} already exists, skipping",
                        account.Name,
                        val.EffectiveDate
                    );
                    _skipped++;
                    continue;
                }

                if (_dryRun)
                {
                    _logger.LogInformation(
                        "[DRY RUN] Would create valuation for '{Account}' on {Date}",
                        account.Name,
                        val.EffectiveDate
                    );
                    continue;
                }

                var result = await _api.CreateValuationAsync(
                    accountId,
                    new CreateValuationRequest
                    {
                        Amount = val.Amount,
                        Currency = val.Currency,
                        EffectiveDate = effectiveDate,
                        Source = val.Source,
                        Notes = val.Notes,
                    }
                );
                if (result is not null)
                {
                    _logger.LogInformation(
                        "  Created valuation for '{Account}' on {Date}: {Amount} {Currency}",
                        account.Name,
                        val.EffectiveDate,
                        val.Amount,
                        val.Currency
                    );
                    _created++;
                }
                else
                {
                    _logger.LogWarning(
                        "  Failed to create valuation for '{Account}' on {Date}",
                        account.Name,
                        val.EffectiveDate
                    );
                    _failed++;
                }
            }
        }
    }

    private async Task LoadPriceHistoryAsync()
    {
        if (_config.PriceHistory.Count == 0)
            return;

        _logger.LogInformation("Loading price history...");

        foreach (var group in _config.PriceHistory)
        {
            if (group.Prices.Count == 0)
                continue;

            if (_dryRun)
            {
                _logger.LogInformation(
                    "[DRY RUN] Would load {Count} prices for {Symbol}",
                    group.Prices.Count,
                    group.Symbol
                );
                continue;
            }

            var request = new BatchCreatePriceHistoryRequest
            {
                Entries = group
                    .Prices.Select(p => new CreatePriceHistoryRequest
                    {
                        Symbol = group.Symbol,
                        Price = p.Price,
                        Currency = group.Currency,
                        AsOfDate = DateOnly.Parse(p.Date),
                        Source = group.Source,
                    })
                    .ToList(),
            };

            var result = await _api.BatchCreatePriceHistoryAsync(request);
            if (result is not null)
            {
                _logger.LogInformation(
                    "  Loaded {Created} new prices for {Symbol} ({Total} submitted, {Skipped} duplicates)",
                    result.Count,
                    group.Symbol,
                    group.Prices.Count,
                    group.Prices.Count - result.Count
                );
                _created += result.Count;
                _skipped += group.Prices.Count - result.Count;
            }
            else
            {
                _logger.LogWarning("  Failed to load prices for {Symbol}", group.Symbol);
                _failed += group.Prices.Count;
            }
        }
    }

    private async Task ApplyAdjustmentsAsync()
    {
        var accountsWithAdjustments = _config
            .Accounts.Where(a => a.Adjustments is not null)
            .ToList();
        if (accountsWithAdjustments.Count == 0)
            return;

        _logger.LogInformation("Applying account adjustments...");

        foreach (var account in accountsWithAdjustments)
        {
            if (!_accountNameToId.TryGetValue(account.Name, out var accountId))
            {
                _logger.LogWarning(
                    "  Account '{Name}' not found, skipping adjustments",
                    account.Name
                );
                _failed++;
                continue;
            }

            var adj = account.Adjustments!;

            if (_dryRun)
            {
                _logger.LogInformation("[DRY RUN] Would adjust account '{Name}'", account.Name);
                continue;
            }

            var result = await _api.UpdateAccountAsync(
                accountId,
                new UpdateAccountRequest
                {
                    Name = adj.Name ?? account.Name,
                    Institution = adj.Institution,
                    Notes = adj.Notes,
                    IsShared = adj.IsShared ?? false,
                }
            );
            if (result is not null)
            {
                _logger.LogInformation("  Adjusted account '{Name}'", account.Name);
                _created++;
            }
            else
            {
                _logger.LogWarning("  Failed to adjust account '{Name}'", account.Name);
                _failed++;
            }
        }
    }
}
