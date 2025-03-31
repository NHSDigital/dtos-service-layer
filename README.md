# Service Layer

[![CI/CD Pull Request](https://github.com/NHSDigital/dtos-service-layer/actions/workflows/cicd-1-pull-request.yaml/badge.svg)](https://github.com/nhs-england-tools/repository-template/actions/workflows/cicd-1-pull-request.yaml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=NHSDigital_dtos-service-layer&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=NHSDigital_dtos-service-layer)

Service Layer

## Table of Contents

- [Service Layer](#service-layer)
  - [Table of Contents](#table-of-contents)
  - [Setup](#setup)
    - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Usage](#usage)
    - [Testing](#testing)
  - [Contacts](#contacts)
  - [Licence](#licence)

## Setup

TODO

Clone the repository

```shell
git clone https://github.com/NHSDigital/dtos-service-layer
cd dtos-service-layer
```

### Prerequisites

The following software packages, or their equivalents, are expected to be installed and configured:

- [Docker](https://www.docker.com/) container runtime or a compatible tool, e.g. [Podman](https://podman.io/),
- [.NET](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) - .NET 9.0
- [Azure functions core tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=macos%2Cisolated-process%2Cnode-v4%2Cpython-v2%2Chttp-trigger%2Ccontainer-apps&pivots=programming-language-csharp)
- [adr-tools](https://github.com/npryce/adr-tools)
- [GNU make](https://www.gnu.org/software/make/) 3.82 or later,

> [!NOTE]<br>
> The version of GNU make available by default on macOS is earlier than 3.82. You will need to upgrade it or certain `make` tasks will fail. On macOS, you will need [Homebrew](https://brew.sh/) installed, then to install `make`, like so:
>
> ```shell
> brew install make
> ```
>
> You will then see instructions to fix your [`$PATH`](https://github.com/nhs-england-tools/dotfiles/blob/main/dot_path.tmpl) variable to make the newly installed version available. If you are using [dotfiles](https://github.com/nhs-england-tools/dotfiles), this is all done for you.

- [Python](https://www.python.org/) required to run Git hooks,
- [`jq`](https://jqlang.github.io/jq/) a lightweight and flexible command-line JSON processor.

## Configuration

Rename the `.env.example` file to `.env` and populate the missing environment variables which are listed at the top of the file.

## Usage

You can run the Azure functions with `make all`

### Testing

The full test suite can be ran with `make test`.

Unit tests can be ran with `make test-unit`

## Contacts

If you are on the NHS England Slack you can contact the team on #mays-team, otherwise you can open a GitHub issue.

## Licence

Unless stated otherwise, the codebase is released under the MIT License. This covers both the codebase and any sample code in the documentation.

Any HTML or Markdown documentation is [© Crown Copyright](https://www.nationalarchives.gov.uk/information-management/re-using-public-sector-information/uk-government-licensing-framework/crown-copyright/) and available under the terms of the [Open Government Licence v3.0](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/).
