# KRT_API
#### O krt_api é um projeto desenvolvido para demonstrar um CRUD completo para cadastro da conta de um cliente, um sistema de mensageria ( RabbitMQ ) entre diferentes aplicações e também uma solução de cache ( Redis ) para aumentar a performace, evitar custos extras e consultas a mais ao banco de dados principal.

1 - **Fluxo do cliente**: o sistema permite que o cliente crie sua conta, visualize seus dados, atualize as informações e também delete a sua conta.
Além disso, o CRUD conta com um endpoint de listagem geral (get-all), que possibilita consultar todas as contas cadastradas, com suporte a filtros por nome, CPF e 
status da conta (ativa/inativa), além de ordenação por data de criação (mais recentes ou mais antigas) e paginação, garantindo uma consulta mais otimizada.
No banco de dados ( PostgreSQL ), são armazenadas as seguintes informações do cliente: id, nome, CPF, status da conta (ativa/inativa), data de criação e data da última atualização.

2 - **Mensageria**: Na solução do **KRT**, existem quatro projetos: **krt_api**, **krt_api_tests**, **krt_cartoes_api** e **krt_prevencao_fraude_api**.
O **krt_api** é o **projeto principal**, responsável por todo o fluxo de CRUD do cliente, pelas configurações de *migrations*, conexão com o banco de dados, implementação das regras de negócio relacionadas à conta de um cliente e produtor das mensagens enviadas para a fila ( criação da conta, edição e exclusão ).
Já os projetos **krt_cartoes_api** e **krt_prevencao_fraude_api** foram criados com o objetivo de atuar como **consumidores de mensagens** no RabbitMQ, processando as mensagens enviadas pelo **krt_api**.
Apesar de serem aplicações simples, contendo basicamente a configuração de um *consumer* e a chamada de um serviço, elas representam de forma prática a **comunicação entre diferentes aplicações**.
Exemplo: quando uma nova conta é criada no **krt_api**, a mensagem é enviada para a fila, e a responsabilidade de tratá-la passa a ser das aplicações consumidoras, que escutam suas respectivas filas e executam ações assim que detectam uma nova mensagem.
Atualmente, os consumidores apenas registram no console que um método foi chamado, mas a estrutura está pronta para evoluir facilmente.

3 - **Redis**: O projeto **krt_api** conta com uma configuração do **Redis** para atuar como sistema de **cache** em todo o fluxo da aplicação, desde a criação até a deleção de uma conta.
Um exemplo: assim que uma conta é criada e salva no banco de dados principal, esses mesmos dados também são armazenados no Redis. A partir desse momento, sempre que algum endpoint precisar acessar informações da conta, a aplicação verifica primeiro se os dados estão disponíveis no Redis.
Se existirem, os dados são retornados diretamente do cache. Caso contrário, a aplicação busca as informações no banco principal e, em seguida, as armazena no Redis para futuras requisições.
Esse mecanismo garante **melhor desempenho nas consultas**, especialmente no endpoint de busca de conta do usuário, que evita ao máximo consultar o banco de dados principal a cada requisição.
Além disso, todo o fluxo foi projetado para **manter a consistência dos dados**: além do tempo de expiração configurado no cache, sempre que a conta é atualizada, as alterações são refletidas tanto no banco de dados principal quanto no Redis.

4 - **Testes**: O projeto **krt_api** também conta com **testes unitários**, localizados no projeto **krt_api_tests**.
Esses testes cobrem todos os **endpoints da aplicação**, validando o comportamento do sistema em diferentes cenários.
Entre os casos testados, estão:
Verificação se os **DTOs** enviados aos endpoints de *create* e *update* são válidos,
garantia de que um **CPF** já vinculado a uma conta ativa não possa ser cadastrado novamente,
Validação de que, ao realizar o *GET* da conta, os **dados sejam retornados do Redis** (cache) e não diretamente do banco principal,
Além de diversos outros cenários que garentem a funcionabilidade da plataforma.

# KRT_API => Regras de négocio

1 - O projeto krt_api conta com diversas regras de validação que garantem a integridade e a consistência dos dados.
Os endpoints que recebem DTOs (como os de POST e PUT) realizam validações antes mesmo de qualquer processamento adicional.
Por exemplo, nome e CPF são campos obrigatórios, e o sistema sempre valida o formato do CPF, que deve ser uma string de 11 caracteres numéricos, sem caracteres especiais e formato válido.
A aplicação também conta com algumas regras, como:
Ao criar uma nova conta, verifica se já existe outra conta ativa com o mesmo CPF, se existir, o cadastro é bloqueado.
Ao atualizar uma conta, caso o usuário tente alterar o CPF, o sistema checa se esse novo CPF já está vinculado a outra conta ativa, se estiver, a aplicação vai barrar o usuário.

2 - Para manter a consistência dos dados e evitar a perda de informações sensíveis, foi implementado um controle das contas quando o usuário solicita a exclusão da conta.
Ou seja, quando um usuário deleta sua conta, ela não é removida do banco de dados, o status é apenas alterado para inativo.
Isso significa que os dados da conta permanecem armazenados como um histórico, mas o usuário perde total acesso a essa conta.
Enquanto existir uma conta com o CPF ativo, o sistema não permite criar outra com o mesmo CPF. No entanto, se a conta estiver inativa, o usuário pode criar uma nova normalmente,
garantindo segurança e controle dos dados sem comprometer a experiência do usuário.

# KRT_API => Tecnologias
- .NET 8
- FluentValidation
- EntityFramework ( PostgreSQL )
- AutoMapper
- FluentAssertions
- RabbitMQ
- Moq
- xUnit
- Redis

# KRT_API => Como rodar o projeto local

1 - Clonar o repositório, clone este repositório em sua máquina local.

2 - Configurar o ambiente .NET
Garanta que você possui um ambiente configurado para rodar aplicações em **.NET 8**.
Caso ainda não tenha, instale o SDK e o runtime disponíveis em:
[https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)

OBS: Todas as configurações que precisam ser adicionadas no appsettings.json devem ser feitas apenas no projeto krt_api.
Os outros projetos não necessitam de nenhuma modificação nas configurações.

3 - Configurar o banco de dados (PostgreSQL)
A aplicação utiliza o **PostgreSQL** como banco principal.
Baixe e instale o PostgreSQL:
[https://www.postgresql.org/download/](https://www.postgresql.org/download/)
Após instalar:
1. Crie um novo banco de dados (ex: `krt_local_base`).
2. Atualize o arquivo `appsettings.json` com suas credenciais:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=krt_local_base;Username=postgres;Password=postgres"
}
```

4 - Configurar o Redis (Cache)
Para rodar o **Redis** localmente, você pode utilizar o Docker.
Crie um arquivo chamado `docker-compose.yml` e adicione:

```yaml
services:
  redis:
    image: redis:7
    container_name: redis
    ports:
      - "6379:6379"
```

Depois, execute:

```bash
docker compose up -d
```

E atualize o `appsettings.json` com a conexão:

```json
"ConnectionStrings": {
  "RedisConnection": "localhost:6379"
}
```
5 - Configurar o RabbitMQ 
Assim como o Redis, o **RabbitMQ** também pode ser executado via Docker.
No mesmo `docker-compose.yml` ou em outro arquivo, adicione:

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"  
      - "15672:15672" 
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
```

Execute:

```bash
docker compose up -d
```
Acesse o painel local do RabbitMQ:
[http://localhost:15672](http://localhost:15672)
Login padrão: **guest** / **guest**

6 - Configurar a execução dos projetos

Abra a solução do projeto no Visual Studio:

1. Clique com o botão direito na **solução** > **Propriedades**
2. Vá em **Configurações de inicialização**
3. Selecione **Vários projetos de inicialização**
4. Marque para iniciar os seguintes projetos:

   * `krt_api`
   * `krt_cartoes_api`
   * `krt_prevencao_fraude_api`

É importante rodar os três **na primeira vez** para que as filas do RabbitMQ sejam criadas corretamente.
Após isso, você pode executar apenas o produtor (`krt_api`) e depois os consumidores conforme necessário.

7 - Rodar o projeto

Agora basta rodar a solução.
O serviço principal (`krt_api`) ficará disponível em:
**[https://localhost:7061](https://localhost:7061)**
Você pode realizar as requisições:
Através de ferramentas como **Postman**, **Insomnia** ou **Swagger**, ou até mesmo
usando o arquivo `krt_api.http`, localizado dentro do próprio projeto **krt_api**.








