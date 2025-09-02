# NOTAS IMPORTANTES

Projeto acadêmico; não é dispositivo médico nem possui aprovação regulatória.

Topics: unity, vr, openxr, urp, meta-quest-2, healthcare, autism, pediatrics, therapy, training.

# **Projeto Anima**

## Primeiros passos

Instale o Git **[(Git for Windows)](https://github.com/git-for-windows/git/releases/download/v2.51.0.windows.1/Git-2.51.0-64-bit.exe)** marcando Add Git to PATH. Se preferir a [versão portable](https://github.com/git-for-windows/git/releases/download/v2.51.0.windows.1/PortableGit-2.51.0-64-bit.7z.exe) (para usar também nos computadores sem acesso administrativo).

Abra o prompt de comando (CMD) e verifique se está devidamente instalado com o comando:

```
git --version
```

Configure sua identidade (uma vez):

```
git config --global user.name "Seu Nome"
git config --global user.email "seu@email.com"
```

(Apenas para computador com acesso administrativo) Git LFS:

```
git lfs install
```

Se em um computador sem acesso administrativo não tiver Git instalado e não puder usar a versão portable: só resta baixar ZIP pelo navegador e fazer commits/push de outra máquina sendo necessário pen drive/upload manual.

## Configurando Ambiente de Trabalho

### **Importante:**
Evite nomes com acentos, espaços ou caracteres especiais nas pastas e arquivos, pois isso pode causar problemas com o Unity ou scripts automatizados.

Escolha o local que quer que o projeto seja salvo, de preferência o mais próximo da raiz possível `(C:)`.

Se solicitado pelo sistema, abra o prompt de comando (CMD) como administrador e, para navegar para a pasta do projeto, basta copiar o endereço da pasta numa janela separada (exemplo: `C:\Users\SeuUsuário\ProjetosUnity\`) e no CMD escrever o comando:

```
cd "endereço copiado"
```

Você pode também, na pasta do projeto e com o clique do botão direito, escolher **"Abrir janela de comando aqui"**.

Agora no CMD basta digitar o comando abaixo:

```bash
git clone https://github.com/Andynee504/Anima.git
```

Isso criará uma nova pasta chamada `Anima` com todos os arquivos do projeto.  
Abra essa pasta no Unity (versão recomendada: **6000.0.35f1**) para começar a trabalhar.

## Conclusão de Configuração

Execute `BaseSetting.bat` uma única vez quando clonar o repositório pela primeira vez.

### **IMPORTANTE**

**Execute `sync_pull.bat` toda vez que começar a trabalhar no projeto para atualizações não se perderem e para que suas alterações não sejam perdidas.**

Na Unity, depois que terminar uma atualização, vá no menu `Tools > Version > Bump Minor` para aplicar a versão nova. (Bump PATCH é APENAS para criação/implementação de novas funcionalidades)

Execute `sync_push.bat` quando terminar de atualizar o projeto para salvar as alterações. Irá aparecer um pop-up para escrever uma descrição breve do que foi feito para ter um changelog atualizado. (exemplo: `Modelo 3D de cadeira adicionado`)

## Mantendo seu repositório atualizado

Dentro da pasta raiz do projeto há um arquivo `sync_push.bat` que deve ser usado quando parar de trabalhar nas atualizações para salvar a versão nova no Git e manter a raiz sempre atualizada ou para que o que foi atualizado seja disponibilizado para um colega. Não se esqueça de executá-lo quando terminar de fazer alterações.