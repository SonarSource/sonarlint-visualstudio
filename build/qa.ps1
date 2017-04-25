[CmdletBinding(PositionalBinding = $false)]
param (
  [parameter(ValueFromRemainingArguments = $true)] $badArgs)

if ($badArgs -ne $null) {
    throw "Bad arguments: $badArgs"
}