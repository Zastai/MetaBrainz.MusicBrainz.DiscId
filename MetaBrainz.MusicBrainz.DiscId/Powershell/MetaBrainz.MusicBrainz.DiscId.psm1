#Require -Version 5.0
using namespace 'MetaBrainz.MusicBrainz.DiscId'

Add-Type -Path "$PSScriptRoot\MetaBrainz.MusicBrainz.DiscId.dll"

$twoSeconds = New-Object -TypeName System.TimeSpan -ArgumentList 0, 0, 2

function Get-AvailableDevices {
  [TableOfContents]::AvailableDevices
}

[string]
function Get-AvailableFeatures {
  [TableOfContents]::AvailableFeatures
}

[string]
function Get-DefaultDevice {
  [TableOfContents]::DefaultDevice
}

[string]
function Get-DiscId {
  [CmdletBinding()]
  param (
    [Parameter(Position = 1, Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Device = [TableOfContents]::DefaultDevice
  )
  (Read-TableOfContents -Device $Device -Features TableOfContents).DiscId
}

[System.Uri]
function Get-SubmissionUrl {
  [CmdletBinding()]
  param (
    [Parameter(Position = 1, Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Device = [TableOfContents]::DefaultDevice
  )
  (Read-TableOfContents -Device $Device -Features TableOfContents).SubmissionUrl
}

[TableOfContents]
function Read-TableOfContents {
  [CmdletBinding()]
  param (
    [Parameter(Position = 1, Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Device = [TableOfContents]::DefaultDevice,
    [DiscReadFeature]
    $Features = [TableOfContents]::AvailableFeatures
  )
  return [TableOfContents]::ReadDisc($Device, $Features)
}

function Show-TableOfContents {
  [CmdletBinding()]
  param (
    [Parameter(Position = 1, Mandatory = $false)]
    [ValidateNotNullOrEmpty()]
    [string]
    $Device = [TableOfContents]::DefaultDevice,
    [switch] $NoTrackISRC          = $false,
    [switch] $NoMediaCatalogNumber = $false,
    [switch] $NoCdText             = $false
  )
  $devices = '';
  $defaultDevice = [TableOfContents]::DefaultDevice
  foreach ($dev in [TableOfContents]::AvailableDevices) {
    if ($devices -ne '') {
      $devices += ', ';
    }
    $devices += $dev;
    if ($dev -eq $defaultDevice) {
      $devices += ' (default)';
    }
  }
  Write-Host ('Available Devices   : {0}' -f $devices)
  Write-Host ('Supported Features  : {0}' -f [TableOfContents]::AvailableFeatures)
  Write-Host ''
  $features = [TableOfContents]::AvailableFeatures
  if ($NoTrackISRC)          { $features = $features -band -bnot [DiscReadFeature]::TrackIsrc          }
  if ($NoMediaCatalogNumber) { $features = $features -band -bnot [DiscReadFeature]::MediaCatalogNumber }
  if ($NoCdText)             { $features = $features -band -bnot [DiscReadFeature]::CdText             }
  try {
    $toc = Read-TableOfContents -Device $Device -Features $features
  }
  catch [System.Exception] {
    Write-Error ('Failed to read table of contents (for device {0}): ${1}' -f $Device, $_)
  }
  if ($toc -eq $null) {
    Write-Host 'No table of contents available.'
  }
  else {
    Write-Host ('CD Device Used      : {0}' -f $toc.DeviceName)
    Write-Host ('Features Requested  : {0}' -f $Features)
    Write-Host ''
    if (($features -band [DiscReadFeature]::MediaCatalogNumber) -ne 0) {
      $mcn = $toc.MediaCatalogNumber
      $mcn = if ($mcn -eq $null) { '* not set *' } else { $mcn }
      Write-Host ('Media Catalog Number: {0}' -f $mcn)
    }
    Write-Host ('MusicBrainz Disc ID : {0}' -f $toc.DiscId)
    Write-Host ('FreeDB Disc ID      : {0}' -f $toc.FreeDbId)
    Write-Host ('Submission URL      : {0}' -f $toc.SubmissionUrl)
    Write-Host ''
    $languages = $toc.TextLanguages
    if ($languages.Count -gt 0) {
      $text = $toc.TextInfo
      if ($text -ne $null) {
        Write-Host 'CD-TEXT Information:'
        $idx = 0
        foreach ($l in $languages) {
          Write-Host ('- Language: {0}' -f $l)
          $ti = $text[$idx++]
          if ($ti.Genre.HasValue) {
            $genre = [string] 
            if ($ti.GenreDescription -ne $null) {
              Write-Host ('  - Genre           : {0} ({1})' -f $ti.Genre.Value, $ti.GenreDescription)
            }
            else {
              Write-Host ('  - Genre           : {0}'       -f $ti.Genre.Value)
            }
          }
          if ($ti.Identification -ne $null) { Write-Host ('  - Identification  : {0}' -f $ti.Identification) }
          if ($ti.ProductCode    -ne $null) { Write-Host ('  - UPC/EAN         : {0}' -f $ti.ProductCode)    }
          if ($ti.Title          -ne $null) { Write-Host ('  - Title           : {0}' -f $ti.Title)          }
          if ($ti.Performer      -ne $null) { Write-Host ('  - Performer       : {0}' -f $ti.Performer)      }
          if ($ti.Lyricist       -ne $null) { Write-Host ('  - Lyricist        : {0}' -f $ti.Lyricist)       }
          if ($ti.Composer       -ne $null) { Write-Host ('  - Composer        : {0}' -f $ti.Composer)       }
          if ($ti.Arranger       -ne $null) { Write-Host ('  - Arranger        : {0}' -f $ti.Arranger)       }
          if ($ti.Message        -ne $null) { Write-Host ('  - Message         : {0}' -f $ti.Message)        }
        }
        Write-Host ''
      }
    }
    if ($toc.Tracks.Count -gt 0) {
      Write-Host 'Tracks:'
      # Check for a "hidden" pre-gap track (FIXME: Only when FirstTrack == 1?)
      $t = $toc.Tracks[$toc.FirstTrack]
      if ($t.StartTime -gt $twoSeconds) {
        Write-Host (' --- Offset: {0,6} ({1,-16}) Length: {2,6} ({3,-16})' -f 150, $twoSeconds, $t.Offset - 150, $t.StartTime.Subtract($twoSeconds))
      }
      $isrcRequested = ($features -band [DiscReadFeature]::TrackIsrc) -ne 0
      foreach ($t in $toc.Tracks) {
        $isrc = $null
        if ($isrcRequested) {
          $isrc = $t.Isrc
          $isrc = if ($isrc -eq $null) { '* not set *' } else { $isrc }
          $isrc = ' ISRC: ' + $isrc
        }
        Write-Host (' {0,2}. Offset: {1,6} ({2,-16}) Length: {3,6} ({4,-16}){5}' -f $t.Number, $t.Offset, $t.StartTime, $t.Length, $t.Duration, $isrc)
        if ($languages.Count -gt 0) {
          $text = $t.TextInfo
          if ($text -ne $null) {
            Write-Host '     CD-TEXT Information:'
            $idx = 0
            foreach ($l in $languages) {
              Write-Host ('     - Language: {0}' -f $l)
              $ti = $text[$idx++]
              if ($ti.Title          -ne $null) { Write-Host ('       - Title           : {0}' -f $ti.Title)          }
              if ($ti.Performer      -ne $null) { Write-Host ('       - Performer       : {0}' -f $ti.Performer)      }
              if ($ti.Lyricist       -ne $null) { Write-Host ('       - Lyricist        : {0}' -f $ti.Lyricist)       }
              if ($ti.Composer       -ne $null) { Write-Host ('       - Composer        : {0}' -f $ti.Composer)       }
              if ($ti.Arranger       -ne $null) { Write-Host ('       - Arranger        : {0}' -f $ti.Arranger)       }
              if ($ti.Message        -ne $null) { Write-Host ('       - Message         : {0}' -f $ti.Message)        }
              if ($ti.Isrc           -ne $null) { Write-Host ('       - ISRC            : {0}' -f $ti.Isrc)           }
            }
          }
        }
      }
    }
  }
}
