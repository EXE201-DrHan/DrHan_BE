# PowerShell script to remove Id, CreatedAt, and UpdatedAt properties from model classes except BaseEntity
$modelsPath = "C:\Users\caotr\source\repos\DrHan\DrHan.Domain\Models"
$files = Get-ChildItem -Path $modelsPath -Filter "*.cs" -Exclude "BaseEntity.cs"

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $modified = $false
    
    # Patterns for properties to remove
    $patterns = @(
        '\s*public\s+Guid\s+Id\s*{\s*get;\s*set;\s*}\s*',
        '\s*public\s+DateTime\?\s+CreatedAt\s*{\s*get;\s*set;\s*}\s*',
        '\s*public\s+DateTime\?\s+UpdatedAt\s*{\s*get;\s*set;\s*}\s*'
    )
    
    $newContent = $content
    foreach ($pattern in $patterns) {
        if ($newContent -match $pattern) {
            $newContent = $newContent -replace $pattern, ''
            $modified = $true
        }
    }
    
    if ($modified) {
        try {
            # Create backup before modifying
            Copy-Item -Path $file.FullName -Destination "$($file.FullName).bak" -ErrorAction Stop
            Set-Content -Path $file.FullName -Value $newContent -ErrorAction Stop
            Write-Host "Removed properties from $($file.Name)"
        } catch {
            Write-Host "Error updating $($file.Name): $_"
        }
    } else {
        Write-Host "No matching properties found in $($file.Name)"
    }
}

Write-Host "Finished removing Id, CreatedAt, and UpdatedAt properties from model classes!"