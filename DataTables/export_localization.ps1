param(
    [string]$Source = (Join-Path $PSScriptRoot 'Data/localization_texts.xlsx'),
    [string]$Output = (Join-Path $PSScriptRoot 'Data/localization_texts.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

function Read-EntryXml {
    param($Archive, [string]$Path)
    $Entry = $Archive.GetEntry($Path)
    if ($null -eq $Entry) { throw "Missing XLSX entry $Path in $Source" }
    $Stream = $Entry.Open()
    try {
        $Document = [System.Xml.XmlDocument]::new()
        $Document.XmlResolver = $null
        $Document.Load($Stream)
        return ,$Document
    }
    finally { $Stream.Dispose() }
}

function Read-CellText {
    param($Cell, $SharedList)
    $Location = $Cell.GetAttribute('r')
    if ($null -ne $Cell.SelectSingleNode('./*[local-name()="f"]')) {
        throw "Texts!$Location contains a formula Use literal text instead"
    }
    $Type = $Cell.GetAttribute('t')
    $Value = $Cell.SelectSingleNode('./*[local-name()="v"]')
    if ($Type -eq 'inlineStr') {
        return (($Cell.SelectNodes('./*[local-name()="is"]/*[local-name()="t"] | ./*[local-name()="is"]/*[local-name()="r"]/*[local-name()="t"]') | ForEach-Object { $_.InnerText }) -join '')
    }
    if ($null -eq $Value) { return '' }
    if ($Type -eq 's') { return $SharedList[[int]$Value.InnerText] }
    if ($Type -eq 'str') { return $Value.InnerText }
    throw "Texts!$Location must be an Excel text cell Numeric boolean error and cached formula cells are not accepted"
}

function Get-ColumnIndex {
    param([string]$Address)
    if ($Address -notmatch '^([A-Z]+)[1-9][0-9]*$') { throw "Invalid cell address $Address" }
    [int]$Column = 0
    foreach ($Character in $Matches[1].ToCharArray()) {
        $Column = $Column * 26 + [int]$Character - [int][char]'A' + 1
    }
    return $Column
}

$Archive = [System.IO.Compression.ZipFile]::OpenRead([System.IO.Path]::GetFullPath($Source))
try {
    $Workbook = Read-EntryXml $Archive 'xl/workbook.xml'
    $Sheet = $Workbook.SelectSingleNode('/*[local-name()="workbook"]/*[local-name()="sheets"]/*[local-name()="sheet"][@name="Texts"]')
    if ($null -eq $Sheet) { throw 'Missing worksheet Texts' }
    $RelationshipId = $Sheet.GetAttribute('id', 'http://schemas.openxmlformats.org/officeDocument/2006/relationships')
    $Relationships = Read-EntryXml $Archive 'xl/_rels/workbook.xml.rels'
    $Relationship = $Relationships.SelectSingleNode('/*[local-name()="Relationships"]/*[local-name()="Relationship"][@Id="' + $RelationshipId + '"]')
    if ($null -eq $Relationship -or $Relationship.GetAttribute('TargetMode') -eq 'External') { throw 'Invalid Texts worksheet relationship' }
    $Target = $Relationship.GetAttribute('Target')
    $SheetPath = ([System.Uri]::new([System.Uri]'http://xlsx/xl/workbook.xml', $Target)).AbsolutePath.TrimStart('/')
    $Document = Read-EntryXml $Archive $SheetPath
    if ($Document.SelectNodes('//*[local-name()="mergeCell"]').Count -gt 0) { throw 'Texts must not contain merged cells' }
    $SharedList = [System.Collections.Generic.List[string]]::new()
    if ($null -ne $Archive.GetEntry('xl/sharedStrings.xml')) {
        $Shared = Read-EntryXml $Archive 'xl/sharedStrings.xml'
        foreach ($Item in $Shared.SelectNodes('/*[local-name()="sst"]/*[local-name()="si"]')) {
            $SharedList.Add(($Item.SelectNodes('./*[local-name()="t"] | ./*[local-name()="r"]/*[local-name()="t"]') | ForEach-Object { $_.InnerText }) -join '')
        }
    }
    $RowList = @($Document.SelectNodes('/*[local-name()="worksheet"]/*[local-name()="sheetData"]/*[local-name()="row"]'))
    $Header = $RowList | Where-Object { $_.GetAttribute('r') -eq '1' }
    if ($null -eq $Header) { throw 'Texts row 1 must contain Key and locale headers' }
    $LocaleDict = [System.Collections.Generic.SortedDictionary[int,string]]::new()
    $LocaleHashList = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($Cell in $Header.SelectNodes('./*[local-name()="c"]')) {
        $Text = Read-CellText $Cell $SharedList
        [int]$Column = Get-ColumnIndex $Cell.GetAttribute('r')
        if ($Column -eq 1) {
            if ($Text -cne 'Key') { throw 'Texts!A1 must be Key' }
            continue
        }
        if ([string]::IsNullOrWhiteSpace($Text)) { continue }
        $Culture = [System.Globalization.CultureInfo]::GetCultureInfo($Text)
        if ($Culture.Name.Length -eq 0 -or $Text -cne $Culture.Name) { throw "Texts header $Text must use canonical CultureInfo name $($Culture.Name)" }
        if (-not $LocaleHashList.Add($Culture.Name)) { throw "Duplicate locale header $Text" }
        $LocaleDict.Add($Column, $Culture.Name)
    }
    if ($null -eq $Header.SelectSingleNode('./*[local-name()="c"][@r="A1"]') -or $LocaleDict.Count -eq 0) { throw 'Texts requires Key in A1 and at least one locale column' }
    $KeyHashList = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    $RecordList = [System.Collections.Generic.List[object]]::new()
    foreach ($Row in $RowList) {
        [int]$RowNumber = [int]$Row.GetAttribute('r')
        if ($RowNumber -eq 1) { continue }
        $CellDict = [System.Collections.Generic.Dictionary[int,string]]::new()
        foreach ($Cell in $Row.SelectNodes('./*[local-name()="c"]')) {
            $Text = Read-CellText $Cell $SharedList
            if ($Text.Length -gt 0) { $CellDict.Add((Get-ColumnIndex $Cell.GetAttribute('r')), $Text) }
        }
        if ($CellDict.Count -eq 0) { continue }
        if (-not $CellDict.ContainsKey(1) -or [string]::IsNullOrWhiteSpace($CellDict[1])) { throw "Texts row $RowNumber is missing Key" }
        $Key = $CellDict[1]
        if ($Key -cne $Key.Trim()) { throw "Texts!A$RowNumber Key contains leading or trailing whitespace" }
        if (-not $KeyHashList.Add($Key)) { throw "Texts!A$RowNumber duplicate Key $Key" }
        foreach ($Column in $CellDict.Keys) {
            if ($Column -ne 1 -and -not $LocaleDict.ContainsKey($Column)) { throw "Texts row $RowNumber column $Column contains data without a locale header" }
        }
        foreach ($Pair in $LocaleDict.GetEnumerator()) {
            if (-not $CellDict.ContainsKey($Pair.Key) -or [string]::IsNullOrWhiteSpace($CellDict[$Pair.Key])) {
                throw "Texts row $RowNumber Key $Key missing translation $($Pair.Value)"
            }
            $RecordList.Add([ordered]@{ Locale = $Pair.Value; Key = $Key; Text = $CellDict[$Pair.Key] })
        }
    }
    if ($RecordList.Count -eq 0) { throw 'Texts must contain at least one translated Key' }
    $Json = ConvertTo-Json -InputObject $RecordList.ToArray() -Depth 4
    $OutputPath = [System.IO.Path]::GetFullPath($Output)
    $Changed = -not [System.IO.File]::Exists($OutputPath) -or [System.IO.File]::ReadAllText($OutputPath) -cne $Json
    if ($Changed) {
        $OutputDirectory = [System.IO.Path]::GetDirectoryName($OutputPath)
        [System.IO.Directory]::CreateDirectory($OutputDirectory) | Out-Null
        $TemporaryPath = Join-Path $OutputDirectory ([System.IO.Path]::GetRandomFileName())
        try {
            [System.IO.File]::WriteAllText($TemporaryPath, $Json, [System.Text.UTF8Encoding]::new($false))
            if ([System.IO.File]::Exists($OutputPath)) { [System.IO.File]::Replace($TemporaryPath, $OutputPath, [System.Management.Automation.Language.NullString]::Value) }
            else { [System.IO.File]::Move($TemporaryPath, $OutputPath) }
        }
        finally {
            if ([System.IO.File]::Exists($TemporaryPath)) { [System.IO.File]::Delete($TemporaryPath) }
        }
    }
    Write-Host "Localization Excel validated $($KeyHashList.Count) keys $($LocaleDict.Count) locales $($RecordList.Count) records changed=$Changed"
}
finally { $Archive.Dispose() }
