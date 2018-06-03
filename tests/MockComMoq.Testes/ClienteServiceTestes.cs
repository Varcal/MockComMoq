using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Bogus;
using Castle.DynamicProxy.Internal;
using Moq;
using Xunit;

namespace MockComMoq.Testes
{
    public class ClienteServiceTestes
    {
        private readonly Faker _faker;
        private readonly ClienteDto _clienteDto;
        private readonly Mock<IClienteRepository> _mockClienteRepository;
        private readonly Mock<IEmailBuilder> _mockEmailBuilder;
        private readonly Mock<IIdFactory> _idFactory;


        public ClienteServiceTestes()
        {
            _faker = new Faker("pt_BR");
            _clienteDto = new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName);
            _mockClienteRepository = new Mock<IClienteRepository>();
            _mockEmailBuilder = new Mock<IEmailBuilder>();
            _idFactory = new Mock<IIdFactory>();
        }


        [Fact(DisplayName = "Deve inserir um cliente")]
        [Trait("Serviços", nameof(Cliente))]
        public void Deve_inserir_um_cliente()
        {
            //Arrange
            _mockClienteRepository.Setup(r => r.Salvar(It.IsAny<Cliente>()));
            _mockEmailBuilder.Setup(e => e.From(It.IsAny<ClienteDto>()))
                .Returns(() => $"{_clienteDto.Nome}.{_clienteDto.Sobrenome}@varcalsys.com.br");
            var clienteService = new ClienteService(_mockEmailBuilder.Object,_mockClienteRepository.Object, _idFactory.Object);

            //Act
            clienteService.Registrar(_clienteDto);

            //Assert
            _mockClienteRepository.VerifyAll();
        }


        [Fact(DisplayName = "Deve inserir um cliente definindo seu e-mail")]
        [Trait("Serviços", nameof(Cliente))]
        public void Deve_inserir_um_cliente_definindo_seu_email()
        {
            //Arrange
            var email = _faker.Person.Email;
            _mockClienteRepository.Setup(r => r.Salvar(It.IsAny<Cliente>()));

            _mockEmailBuilder
                .Setup(e => e.CriarEmail(It.IsAny<ClienteDto>(), out email))
                .Returns(true);

            var clienteService = new ClienteService(_mockEmailBuilder.Object, _mockClienteRepository.Object, _idFactory.Object);

            //Act
            clienteService.RegistrarDefinindoEmail(_clienteDto);

            //Assert
            _mockClienteRepository.VerifyAll();
        }


        [Fact(DisplayName = "Deve inserir clientes em lote")]
        [Trait("Serviços", nameof(Cliente))]
        public void Deve_inserir_clientes_em_lote()
        {
            //Arrange
            var clientesDto = new List<ClienteDto>
            {
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName)
            };

            var mockClienteRepository = new Mock<IClienteRepository>();
            var mockEmailBuilder = new Mock<IEmailBuilder>();
            var clienteService = new ClienteService(mockEmailBuilder.Object, mockClienteRepository.Object, _idFactory.Object);

            //Act
            clienteService.RegistrarEmLote(clientesDto);

            //Assert
            mockClienteRepository.Verify(x=> x.Salvar(It.IsAny<Cliente>()), Times.Exactly(clientesDto.Count));
        }


        [Fact(DisplayName = "Deve lançar um exceçao se endereço de email não for criar")]
        [Trait("Serviços", nameof(Cliente))]
        public void Deve_lança_uma_execeção_se_endereço_de_email_não_for_criado()
        {
            //Arrange
            var clienteService = new ClienteService(_mockEmailBuilder.Object, _mockClienteRepository.Object, _idFactory.Object);
            _mockEmailBuilder.Setup(e => e.From(It.IsAny<ClienteDto>())).Returns(() => null);

            //Act
            Action action = () => clienteService.Registrar(_clienteDto);

            //Assert
            Assert.Throws<ArgumentException>(action);
        }


        [Fact(DisplayName = "Criar a entidade com id")]
        [Trait("Serviços", nameof(Cliente))]
        public void Criar_entidade_com_id()
        {
            //Arrange
            var clientesDto = new List<ClienteDto>
            {
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName),
                new ClienteDto(_faker.Person.FirstName, _faker.Person.LastName)
            };

            var clienteService = new ClienteService(_mockEmailBuilder.Object, _mockClienteRepository.Object, _idFactory.Object);
            _mockEmailBuilder
                .Setup(e => e.From(It.IsAny<ClienteDto>()))
                .Returns(() => $"{_clienteDto.Nome}.{_clienteDto.Sobrenome}@varcalsys.com.br");

            var i = 1;
            _idFactory.Setup(f => f.Criar()).Returns(() => i).Callback(() => i++);

            //Act   
            clienteService.RegistrarComId(clientesDto);

            //Assert
            _mockClienteRepository.Verify(x=>x.Salvar(It.IsAny<Cliente>()), Times.Exactly(clientesDto.Count));
        }
    }


    public class Cliente
    {
        public int Id { get; private set; }
        public string Nome { get; private set; }
        public string Sobrenome { get; private set; }
        public string Email { get; private set; }

        public Cliente(string nome, string sobrenome)
        {
            Nome = nome;
            Sobrenome = sobrenome;
        }

        internal void IncluirEmail(string email)
        {
            Email = email;
        }
    }

    public class ClienteService
    {
        private readonly IEmailBuilder _emailBuilder;
        private readonly IClienteRepository _clienteRepository;
        private readonly IIdFactory _idFactory;

        public ClienteService(IEmailBuilder emailBuilder, IClienteRepository clienteRepository, IIdFactory idFactory)
        {
            _emailBuilder = emailBuilder;
            _clienteRepository = clienteRepository;
            _idFactory = idFactory;
        }

        public void Registrar(ClienteDto clienteDto)
        {
            var cliente = new Cliente(clienteDto.Nome, clienteDto.Sobrenome);

            var email = _emailBuilder.From(clienteDto);

            if(string.IsNullOrWhiteSpace(email))
                throw new ArgumentException();

            cliente.IncluirEmail(email);
            _clienteRepository.Salvar(cliente);
        }

        public void RegistrarEmLote(IList<ClienteDto> clientesDto)
        {
            foreach (var clienteDto in clientesDto)
            {
                var cliente = new Cliente(clienteDto.Nome, clienteDto.Sobrenome);
                _clienteRepository.Salvar(cliente);
            }
        }

        public void RegistrarDefinindoEmail(ClienteDto clienteDto)
        {
            var cliente = new Cliente(clienteDto.Nome, clienteDto.Sobrenome);

            _emailBuilder.CriarEmail(clienteDto, out string enderecoEmail);

            if (string.IsNullOrWhiteSpace(enderecoEmail))
                throw new ArgumentException();

            cliente.IncluirEmail(enderecoEmail);
            _clienteRepository.Salvar(cliente);
        }

        public void RegistrarComId(IList<ClienteDto> clienteDtoList)
        {

            foreach (var clienteDto in clienteDtoList)
            {
                var cliente = new Cliente(clienteDto.Nome, clienteDto.Sobrenome);

                var id = _idFactory.Criar();
                cliente.GetType().GetProperty("Id").SetValue(cliente, id);

                _clienteRepository.Salvar(cliente);
            }
        }
    }

    public interface IIdFactory
    {
        int Criar();
    }

    public interface IEmailBuilder
    {
        string From(ClienteDto clienteDto);
        bool CriarEmail(ClienteDto clienteDto, out string email);
    }

    public interface IClienteRepository
    {
        void Salvar(Cliente cliente);
    }

    public class ClienteDto
    {
        public string Nome { get; private set; }
        public string Sobrenome { get; private set; }

        public ClienteDto(string nome, string sobreNome)
        {
            Nome = nome;
            Sobrenome = sobreNome;
        }
    }
}
