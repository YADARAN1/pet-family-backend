dotnet-ef database drop -f -c AccountsWriteDbContext -p .\src\Accounts\PetFamily.Accounts.Infrastructure\ -s .\src\Web\
dotnet-ef database drop -f -c VolunteersWriteDbContext -p .\src\VolunteerManagement\PetFamily.VolunteerManagement.Infrastructure\ -s .\src\Web\
dotnet-ef database drop -f -c SpeciesWriteDbContext -p .\src\Species\PetFamily.Species.Infrastructure\ -s .\src\Web\

dotnet-ef migrations remove -c AccountsWriteDbContext -p .\src\Accounts\PetFamily.Accounts.Infrastructure\ -s .\src\Web\
dotnet-ef migrations remove -c VolunteersWriteDbContext -p .\src\VolunteerManagement\PetFamily.VolunteerManagement.Infrastructure\ -s .\src\Web\
dotnet-ef migrations remove -c SpeciesWriteDbContext -p .\src\Species\PetFamily.Species.Infrastructure\ -s .\src\Web\

dotnet-ef migrations add init -c AccountsWriteDbContext -p .\src\Accounts\PetFamily.Accounts.Infrastructure\ -s .\src\Web\
dotnet-ef migrations add init -c VolunteersWriteDbContext -p .\src\VolunteerManagement\PetFamily.VolunteerManagement.Infrastructure\ -s .\src\Web\
dotnet-ef migrations add init -c SpeciesWriteDbContext -p .\src\Species\PetFamily.Species.Infrastructure\ -s .\src\Web\

dotnet-ef database update -c AccountsWriteDbContext -p .\src\Accounts\PetFamily.Accounts.Infrastructure\ -s .\src\Web\
dotnet-ef database update -c VolunteersWriteDbContext -p .\src\VolunteerManagement\PetFamily.VolunteerManagement.Infrastructure\ -s .\src\Web\
dotnet-ef database update -c SpeciesWriteDbContext -p .\src\Species\PetFamily.Species.Infrastructure\ -s .\src\Web\

pause