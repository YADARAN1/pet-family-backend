using System.Data;
using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using PetFamily.Core.Messaging;
using PetFamily.Core.Providers.FileProvider;
using PetFamily.SharedKernel;
using PetFamily.SharedKernel.EntityIds;
using PetFamily.SharedKernel.ValueObjects;
using PetFamily.VolunteerManagement.Application;
using PetFamily.VolunteerManagement.Application.Commands.AddFilesPet;
using PetFamily.VolunteerManagement.Domain;
using PetFamily.VolunteerManagement.Domain.Entities;
using PetFamily.VolunteerManagement.Domain.Enums;
using PetFamily.VolunteerManagement.Domain.ValueObjects;
using Xunit;

namespace PetFamily.Application.UnitTests;

public class AddPhotosToPetTest
{
    private readonly Mock<IVolunteersRepository> _volunteerRepositoryMock = new();
    private readonly Mock<IValidator<AddPhotosToPetCommand>> _validatorMock = new();
    private readonly Mock<IVolunteerUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<AddPhotosToPetHandler>> _loggerMock = new();
    private readonly Mock<IFileProvider> _fileProviderMock = new();
    private readonly Mock<IDbTransaction> _dbTransactionMock = new();
    private readonly Mock<IMessageQueue<IEnumerable<FilePath>>> _messageQueue = new();

    [Fact]
    public async void Execute_Should_Upload_Files_To_Pet()
    {
        // arrange
        var ct = new CancellationToken();

        var volunteer = CreateVolunteerWithPets(1);
        var volunteerId = volunteer.Id;
        var petId = volunteer.Pets[0].Id;

        var fileContents = new List<FileContent>
        {
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files"),
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files")
        };
        var createFileCommand = fileContents.Select(f => new CreateFileCommand(f.Stream, f.File.Path));
        var command = new AddPhotosToPetCommand(volunteerId, petId, createFileCommand);

        _validatorMock.Setup(v => v.ValidateAsync(command, ct))
            .ReturnsAsync(new ValidationResult());
        
        _volunteerRepositoryMock.Setup(v => v.Save(It.IsAny<Volunteer>(), ct))
            .Returns(Task.CompletedTask);

        _volunteerRepositoryMock.Setup(v => v.GetById(volunteerId, ct))
            .ReturnsAsync(Result.Success<Volunteer, Error>(volunteer));

        _unitOfWorkMock.Setup(u => u.SaveChanges(ct))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.BeginTransaction(ct))
            .ReturnsAsync(_dbTransactionMock.Object);

        _messageQueue.Setup(m => m.WriteAsync(fileContents.Select(f => f.File), ct))
            .Returns(Task.CompletedTask);

        var returnedFilePaths = fileContents
            .Select(f => f.File)
            .Select(f => f.Path).ToList();

        _fileProviderMock.Setup(f => f.UploadFiles(It.IsAny<List<FileContent>>(), ct))
            .ReturnsAsync(Result.Success<IEnumerable<string>, Error>(returnedFilePaths));

        var handler = new AddPhotosToPetHandler(
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _volunteerRepositoryMock.Object,
            _fileProviderMock.Object,
            _messageQueue.Object,
            _loggerMock.Object);

        // act
        var resultHandle = await handler.Execute(command, ct);

        // assert
        resultHandle.IsSuccess.Should().BeTrue();
        resultHandle.Value.Equals(petId.Id).Should().BeTrue();
    }

    [Fact]
    public async Task Execute_With_Invalid_Command_Should_Return_Validation_Errors()
    {
        // arrange
        var ct = new CancellationToken();

        var volunteer = CreateVolunteerWithPets(1);
        var volunteerId = volunteer.Id;
        var petId = volunteer.Pets[0].Id;

        var fileContents = new List<FileContent>
        {
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files"),
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files")
        };

        var fileCommands = fileContents.Select(f => new CreateFileCommand(f.Stream, f.File));
        var command = new AddPhotosToPetCommand(volunteerId, petId, fileCommands);

        var errorValidate = Errors.General.ValueIsInvalid(nameof(command.Files)).Serialize();
        var validationFailures = new List<ValidationFailure>
        {
            new(nameof(command.Files), errorValidate),
        };
        var validationResult = new ValidationResult(validationFailures);

        _validatorMock.Setup(v => v.ValidateAsync(command, ct))
            .ReturnsAsync(validationResult);

        _fileProviderMock.Setup(f => f.UploadFiles(It.IsAny<List<FileContent>>(), ct))
            .ReturnsAsync(
                Result.Success<IEnumerable<string>, Error>(fileContents.Select(f => f.File.Path))
            );

        _messageQueue.Setup(m => m.WriteAsync(fileContents.Select(f => f.File), ct))
            .Returns(Task.CompletedTask);

        var handler = new AddPhotosToPetHandler(
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _volunteerRepositoryMock.Object,
            _fileProviderMock.Object,
            _messageQueue.Object,
            _loggerMock.Object
        );

        // act
        var result = await handler.Execute(command, ct);

        // assert
        result.IsFailure.Should().BeTrue();
        result.Error.First().InvalidField.Should().Be(nameof(command.Files));
    }

    [Fact]
    public async Task Execute_Volunteer_Not_Found_Should_Return_Not_Found_Error()
    {
        // arrange
        var ct = new CancellationToken();

        var volunteerId = VolunteerId.NewId();
        var petId = PetId.NewId();

        var fileContents = new List<FileContent>
        {
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files"),
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files")
        };

        var fileCommands = fileContents.Select(f => new CreateFileCommand(f.Stream, f.File));
        
        var command = new AddPhotosToPetCommand(volunteerId, petId, fileCommands);

        _validatorMock.Setup(v => v.ValidateAsync(command, ct))
            .ReturnsAsync(new ValidationResult());

        _volunteerRepositoryMock.Setup(v => v.GetById(volunteerId, ct))
            .ReturnsAsync(Result.Failure<Volunteer, Error>(Errors.General.NotFound(volunteerId.Id)));
        
        _messageQueue.Setup(m => m.WriteAsync(fileContents.Select(f => f.File), ct))
            .Returns(Task.CompletedTask);
        
        var handler = new AddPhotosToPetHandler(
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _volunteerRepositoryMock.Object,
            _fileProviderMock.Object,
            _messageQueue.Object,
            _loggerMock.Object
        );

        // act
        var result = await handler.Execute(command, ct);

        // assert
        result.IsFailure.Should().BeTrue();
        var error = result.Error.First();
        error.Code.Should().Be("record.not.found");
        error.Message.Should().Contain("record not found for Id");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Execute_FileUploadFails_Should_Return_File_Upload_Error()
    {
        // arrange
        var ct = new CancellationToken();

        var volunteer = CreateVolunteerWithPets(1);
        var volunteerId = volunteer.Id;
        var petId = volunteer.Pets[0].Id;

        var command = new AddPhotosToPetCommand(volunteerId, petId, CreateAddPhotosToPetCommand());

        _validatorMock.Setup(v => v.ValidateAsync(command, ct))
            .ReturnsAsync(new ValidationResult());

        _volunteerRepositoryMock.Setup(v => v.GetById(volunteerId, ct))
            .ReturnsAsync(Result.Success<Volunteer, Error>(volunteer));

        _unitOfWorkMock.Setup(u => u.BeginTransaction(ct))
            .ReturnsAsync(_dbTransactionMock.Object);

        _fileProviderMock.Setup(f => f.UploadFiles(It.IsAny<List<FileContent>>(), ct))
            .ReturnsAsync(Result.Failure<IEnumerable<string>, Error>(
                Error.Failure("failed.add.photos", "Failed add photos to pet"))
            );

        var loggerMock = new Mock<ILogger<AddPhotosToPetHandler>>();

        var handler = new AddPhotosToPetHandler(
            _unitOfWorkMock.Object,
            _validatorMock.Object,
            _volunteerRepositoryMock.Object,
            _fileProviderMock.Object,
            _messageQueue.Object,
            loggerMock.Object
        );

        // act
        var result = await handler.Execute(command, ct);
        // assert
        result.IsFailure.Should().BeTrue();
        var error = result.Error.First();
        error.Code.Should().Be("failed.add.photos");
        error.Message.Should().Contain("Failed add photos to pet");
        error.Type.Should().Be(ErrorType.Failure);
    }

    private IEnumerable<CreateFileCommand> CreateAddPhotosToPetCommand()
    {
        var fileContents = new List<FileContent>
        {
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files"),
            new(new MemoryStream(), FilePath.Create(Guid.NewGuid() + ".png").Value, "files")
        };
        return fileContents.Select(f => new CreateFileCommand(f.Stream, f.File.Path));
    }

    private Volunteer CreateVolunteerWithPets(int petCount)
    {
        var volunteer = new Volunteer(
            VolunteerId.NewId(),
            FullName.Create("John", "Doe", "sdfsfws").Value,
            Description.Create("General Description").Value,
            AgeExperience.Create(5).Value,
            PhoneNumber.Create("7234567890").Value);

        for (int i = 0; i < petCount; i++)
        {
            var pet = new Pet(
                PetId.NewId(),
                NickName.Create($"Pet {i + 1}").Value,
                Description.Create("General Description").Value,
                Description.Create("Health Information").Value,
                Address.Create("address", "address", "address", "address").Value,
                PetPhysicalAttributes.Create(10, 20).Value,
                Guid.NewGuid(),
                BreedId.NewId(),
                PhoneNumber.Create("7234567890").Value,
                DateTime.Now.AddYears(-1),
                true,
                true,
                HelpStatusPet.LookingForHome,
                DateTime.Now,
                [],
                []
            );
            volunteer.AddPet(pet);
        }

        return volunteer;
    }
}