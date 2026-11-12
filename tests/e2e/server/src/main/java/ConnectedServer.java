/*
 * SonarLint for Visual Studio
 * Copyright (C) SonarSource Sàrl
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

import com.sonar.orchestrator.config.Configuration;
import com.sonar.orchestrator.junit5.OrchestratorExtension;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardCopyOption;
import java.time.Duration;

/** Starts a disposable real server on loopback; never exports credentials as evidence. */
public final class ConnectedServer {
  public static void main(String[] args) throws Exception {
    Path privateDirectory = Path.of(args[0]);
    Files.createDirectories(privateDirectory);
    var config = Configuration.builder().addEnvVariables().addSystemProperties()
      .setProperty("java.home", System.getenv("JAVA_HOME"))
      .setProperty("orchestrator.artifactory.url", "https://repox.jfrog.io/repox")
      .setProperty("orchestrator.artifactory.accessToken", System.getenv("ARTIFACTORY_PASSWORD"))
      .setProperty("orchestrator.artifactory.apiKey", System.getenv("ARTIFACTORY_PASSWORD"));
    var server = OrchestratorExtension.builder(config.build())
      .setSonarVersion("25.11.0.114957")
      .setServerProperty("sonar.web.host", "127.0.0.1")
      .defaultForceAuthentication().keepBundledPlugins().useDefaultAdminCredentialsForBuilds(true).build();
    var stopped = new java.util.concurrent.atomic.AtomicBoolean();
    Runnable stop = () -> { if (stopped.compareAndSet(false, true)) { server.stop(); } };
    Runtime.getRuntime().addShutdownHook(new Thread(stop));
    try {
      server.start();
      // getAppLogs() is <home>/logs/sonar.log; export its directory so web/ce/es logs are found too.
      String logs = server.getServer().getAppLogs().getParentFile().getAbsolutePath();
      String info = "{\"url\":\"" + server.getServer().getUrl() + "\",\"token\":\"" + server.getDefaultAdminToken()
        + "\",\"logs\":\"" + logs.replace("\\", "\\\\") + "\"}";
      // Publish atomically: the launcher script treats the file's existence as readiness.
      Path pending = privateDirectory.resolve("server.json.tmp");
      Files.writeString(pending, info);
      Files.move(pending, privateDirectory.resolve("server.json"), StandardCopyOption.ATOMIC_MOVE);
      System.out.println("Disposable SonarQube is ready.");
      long deadline = System.nanoTime() + Duration.ofMinutes(30).toNanos();
      while (!Files.exists(privateDirectory.resolve("stop")) && System.nanoTime() < deadline) { Thread.sleep(500); }
    } finally { stop.run(); }
  }
}
